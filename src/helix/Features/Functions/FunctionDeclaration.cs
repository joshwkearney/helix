using System.Collections.Immutable;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Features.FlowControl;
using Helix.Features.Functions;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private FunctionParseSignature FunctionSignature() {
            var start = this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.OpenParenthesis);
            this.scope = this.scope.Append(funcName);

            var pars = ImmutableList<ParseFunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                bool isWritable;
                Token parStart;

                if (this.Peek(TokenKind.VarKeyword)) {
                    parStart = this.Advance(TokenKind.VarKeyword);
                    isWritable = true;
                }
                else {
                    parStart = this.Advance(TokenKind.LetKeyword);
                    isWritable = false;
                }

                var parName = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.AsKeyword);

                var parType = this.TopExpression(null);
                var parLoc = parStart.Location.Span(parType.Location);

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parLoc, parName, parType, isWritable));
            }

            this.scope = this.scope.Pop();

            var end = this.Advance(TokenKind.CloseParenthesis);
            var returnType = new VoidLiteral(end.Location) as ISyntaxTree;

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                returnType = this.TopExpression(null);
            }

            var loc = start.Location.Span(returnType.Location);
            var sig = new FunctionParseSignature(loc, funcName, returnType, pars);

            return sig;
        }

        private IDeclaration FunctionDeclaration() {
            var block = new BlockBuilder();
            var sig = this.FunctionSignature();

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.scope = this.scope.Append(sig.Name).Append("%body");
            var body = this.TopExpression(block);            

            this.Advance(TokenKind.Semicolon);
            this.scope = this.scope.Pop();
            block.Statements.Add(body);

            return new FunctionParseDeclaration(
                sig.Location.Span(body.Location), 
                sig,
                block.Statements,
                body);
        }
    }
}

namespace Helix.Features.Functions {
    public record FunctionParseDeclaration : IDeclaration {
        private readonly FunctionParseSignature signature;
        private readonly IReadOnlyList<ISyntaxTree> body;
        private readonly ISyntaxTree retExpr;

        public TokenLocation Location { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, 
            IReadOnlyList<ISyntaxTree> body, ISyntaxTree retExpr) {

            this.Location = loc;
            this.signature = sig;
            this.body = body;
            this.retExpr = retExpr;
        }

        public void DeclareNames(SyntaxFrame types) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location, 
                this.signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareName(this.signature, types);
        }

        public void DeclareTypes(SyntaxFrame types) {
            var sig = this.signature.ResolveNames(types);

            // Declare this function
            types.Functions[sig.Path] = sig;
        }
        
        public IDeclaration CheckTypes(SyntaxFrame types) {
            var path = types.ResolvePath(this.Location.Scope, this.signature.Name);
            var sig = types.Functions[path];
            var bodyExpr = this.retExpr;

            if (sig.ReturnType == PrimitiveType.Void) {
                bodyExpr = new VoidLiteral(this.Location);
            }

            // Set the scope for type checking the body
            types = new SyntaxFrame(types);

            // Declare parameters
            FunctionsHelper.DeclareParameters(this.Location, sig, types);

            // Get a variable name for returns
            var returnName = "$return";
            var returnPath = sig.Path.Append(returnName);
            var flow = new FlowRewriter();

            // Assign the body return value to our return variable
            // at the end of the state machine
            var rewriteBlock = new BlockSyntax(
                this.Location, 
                this.body
                    .Append(new AssignmentStatement(
                        this.retExpr.Location,
                        new VariableAccessParseSyntax(this.retExpr.Location, returnName),
                        bodyExpr))
                    .ToList());

            // Reserve a state for returns
            int returnState = flow.NextState++;
            flow.ReturnState = returnState;

            // Rewrite the flow of this function
            rewriteBlock.RewriteNonlocalFlow(types, flow);

            // Add the return state
            flow.ConstantStates.Add(returnState, new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = flow.NextState
            });

            // Remove extra states that serve no purpose
            flow.OptimizeStates();

            // Declare our return variable
            var returnSig = new VariableSignature(returnPath, sig.ReturnType, true);
            types.Variables[returnPath] = returnSig;
            types.SyntaxValues[returnPath] = new DummySyntax(this.retExpr.Location);

            // Check types
            var body = new StateMachineSyntax(this.retExpr.Location, flow)
                .CheckTypes(types)
                .ToRValue(types);

            // Test check the expression that will return form our function 
            var retTest = new VariableAccessParseSyntax(this.retExpr.Location, returnName)
                .CheckTypes(types);

            // Make sure we're not capturing a stack-allocated variable
            if (types.CapturedVariables[retTest].Contains(new IdentifierPath("$stack"))) {
                throw new TypeCheckingException(
                    this.retExpr.Location,
                    "Dangling Pointer on Return Value",
                    "The return value for this function references stack-allocated memory.");
            }

#if DEBUG
            // Debug check: make sure that every syntax tree has a return type
            foreach (var expr in body.GetAllChildren()) {
                if (!types.ReturnTypes.ContainsKey(expr)) {
                    throw new Exception("Compiler assertion failed: syntax tree does not have a return type");
                }
            }

            // Debug check: make sure that every syntax tree has captured variables
            foreach (var expr in body.GetAllChildren()) {
                if (!types.CapturedVariables.ContainsKey(expr)) {
                    throw new Exception("Compiler assertion failed: syntax tree does not have any captured variables");
                }
            }
#endif

            return new FunctionDeclaration(this.Location, sig, body, returnPath);
        }

        public void GenerateCode(SyntaxFrame types, ICWriter writer) => throw new InvalidOperationException();
    }

    public record FunctionDeclaration : IDeclaration {
        private readonly IdentifierPath returnVar;
        private readonly ISyntaxTree body;

        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, 
            ISyntaxTree body, IdentifierPath returnVar) {

            this.Location = loc;
            this.Signature = sig;
            this.body = body;
            this.returnVar = returnVar;
        }

        public void DeclareNames(SyntaxFrame names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(SyntaxFrame paths) {
            throw new InvalidOperationException();
        }

        public IDeclaration CheckTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public void GenerateCode(SyntaxFrame types, ICWriter writer) {
            writer.ResetTempNames();

            var returnType = this.Signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.Signature.ReturnType);

            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter() { 
                    Type = writer.ConvertType(x.Type),
                    Name = writer.GetVariableName(this.Signature.Path.Append(x.Name))
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.Signature.Path);
            var body = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, body);
            var returnName = writer.GetVariableName(this.returnVar);

            // Declare our return variable
            bodyWriter.WriteStatement(new CVariableDeclaration() {
                Name = returnName,
                Type = writer.ConvertType(this.Signature.ReturnType)
            });

            // Generate the body
            var retExpr = this.body.GenerateCode(types, bodyWriter);

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteEmptyLine();
                bodyWriter.WriteStatement(new CReturn() { 
                    Target = new CVariableLiteral(returnName)
                });
            }

            var decl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars,
                Body = body
            };

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);

            writer.WriteDeclaration4(decl);
            writer.WriteDeclaration4(new CEmptyLine());
        }
    }
}
