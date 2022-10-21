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
            BlockSyntax body;

            this.scope = this.scope.Append(sig.Name);

            if (this.TryAdvance(TokenKind.Yields)) {
                var expr = this.TopExpression(block);

                body = new BlockSyntax(expr.Location, new[] { expr });
            }
            else {
                body = this.TopBlock();
            }

            this.Advance(TokenKind.Semicolon);
            this.scope = this.scope.Pop();
            block.Statements.Add(body);

            return new FunctionParseDeclaration(sig.Location.Span(body.Location), sig, body);
        }
    }
}

namespace Trophy.Features.Functions {
    public record FunctionParseDeclaration : IDeclaration {
        private readonly FunctionParseSignature signature;
        private readonly ISyntaxTree body;

        public TokenLocation Location { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, ISyntaxTree body) {
            this.Location = loc;
            this.signature = sig;
            this.body = body;
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
            var body = this.body;

            // If this function returns void, wrap the body so we don't get weird type errors
            if (sig.ReturnType == PrimitiveType.Void) {
                body = new BlockSyntax(body.Location, new ISyntaxTree[] {
                    body, new VoidLiteral(body.Location)
                });
            }

            // Set the scope for type checking the body
            types = new SyntaxFrame(types);

            // Declare parameters
            FunctionsHelper.DeclareParameters(this.Location, sig, types);

            body = body
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(sig.ReturnType, types);

            return new FunctionDeclaration(this.Location, sig, body);
        }

        public void GenerateCode(ICWriter writer) => throw new InvalidOperationException();
    }

    public record FunctionDeclaration : IDeclaration {
        public FunctionSignature Signature { get; }

        public ISyntaxTree Body { get; }

        public TokenLocation Location { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, ISyntaxTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.Body = body;
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

        public void GenerateCode(ICWriter writer) {
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
            var retExpr = this.Body.GenerateCode(bodyWriter);

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteEmptyLine();
                bodyWriter.WriteStatement(new CReturn() { 
                    Target = retExpr
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
