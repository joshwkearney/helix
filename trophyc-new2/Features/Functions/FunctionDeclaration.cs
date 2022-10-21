using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Functions;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Features.Variables;

namespace Trophy.Parsing {
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

            this.scope = this.scope.Append(sig.Name);
            Advance(TokenKind.Yields);

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

namespace Trophy.Features.Functions {
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
            var decl = new ExternFunctionDeclaration(this.Location, sig);

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

            var flow = new FlowRewriter();
            var rewriteBlock = new BlockSyntax(this.Location, this.body);

            // Reserve a state for returns
            int returnState = flow.NextState++;
            flow.BreakState = returnState;

            // Rewrite the flow of this function
            rewriteBlock.RewriteNonlocalFlow(types, flow);

            // Get a variable name for returns
            var returnName = types.GetVariableName();
            var returnPath = sig.Path.Append(returnName);
            var returnSig = new VariableSignature(returnPath, sig.ReturnType, true);

            // Add the return state
            flow.ConstantStates.Add(returnState, new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = flow.NextState
            });

            // Remove extra states that serve no purpose
            flow.OptimizeStates();

            // Declare our return variable
            types.Variables[returnPath] = returnSig;
            types.Trees[returnPath] = new DummySyntax(this.retExpr.Location);

            var body = new StateMachineSyntax(this.retExpr.Location, flow)
                .CheckTypes(types)
                .ToRValue(types);

            bodyExpr = bodyExpr
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(sig.ReturnType, types);

            return new FunctionDeclaration(this.Location, sig, body, bodyExpr, returnPath);
        }

        public void GenerateCode(SyntaxFrame types, ICWriter writer) => throw new InvalidOperationException();
    }

    public record FunctionDeclaration : IDeclaration {
        private readonly IdentifierPath returnVar;
        private readonly ISyntaxTree body;
        private readonly ISyntaxTree retExpr;

        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, 
            ISyntaxTree body, ISyntaxTree retExpr, IdentifierPath returnVar) {

            this.Location = loc;
            this.Signature = sig;
            this.body = body;
            this.retExpr = retExpr;
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

            bodyWriter.WriteStatement(new CAssignment() { 
                Left = new CVariableLiteral(returnName),
                Right = this.retExpr.GenerateCode(types, bodyWriter)
            });

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
