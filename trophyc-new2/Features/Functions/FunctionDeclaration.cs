using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Analysis.Unification;
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

                var parType = this.TopExpression();
                var parLoc = parStart.Location.Span(parType.Location);

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parLoc, parName, parType, isWritable));
            }

            var end = this.Advance(TokenKind.CloseParenthesis);
            var returnType = new VoidLiteral(end.Location) as ISyntax;

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                returnType = this.TopExpression();
            }

            var loc = start.Location.Span(returnType.Location);
            var sig = new FunctionParseSignature(loc, funcName, returnType, pars);

            return sig;
        }

        private IDeclaration FunctionDeclaration() {
            var start = this.tokens[this.pos];
            var sig = this.FunctionSignature();
            var end = this.Advance(TokenKind.Yields);

            var body = this.TopExpression();
            var loc = start.Location.Span(end.Location);

            this.Advance(TokenKind.Semicolon);

            return new FunctionParseDeclaration(loc, sig, body);
        }
    }
}

namespace Trophy.Features.Functions {
    public record FunctionParseDeclaration : IDeclaration {
        private readonly FunctionParseSignature signature;
        private readonly ISyntax body;

        public TokenLocation Location { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, ISyntax body) {
            this.Location = loc;
            this.signature = sig;
            this.body = body;
        }

        public void DeclareNames(INamesRecorder names) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location, 
                this.signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareSignatureNames(this.signature, names);
        }

        public void DeclareTypes(ITypesRecorder types) {
            var sig = this.signature.ResolveNames(types);

            FunctionsHelper.DeclareSignaturePaths(sig, types);
        }

        public IDeclaration CheckTypes(ITypesRecorder types) {
            var path = types.CurrentScope.Append(this.signature.Name);
            var sig = types.GetFunction(path);
            var body = this.body;

            // If this function returns void, wrap the body so we don't get weird type errors
            if (sig.ReturnType == PrimitiveType.Void) {
                body = new BlockSyntax(this.body.Location, new ISyntax[] {
                    body, new VoidLiteral(body.Location)
                });
            }

            // Set the scope for type checking the body
            types = types.WithScope(sig.Path);

            // Make sure the body is an rvalue
            if (!body.CheckTypes(types).ToRValue(types).TryGetValue(out body)) {
                throw TypeCheckingErrors.RValueRequired(this.body.Location);
            }

            var bodyType = types.GetReturnType(body);

            // Make sure the return type matches the body's type
            if (types.TryUnifyTo(body, bodyType, sig.ReturnType).TryGetValue(out var newBody)) {
                body = newBody;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.Location, sig.ReturnType, bodyType);
            }

            return new FunctionDeclaration(this.Location, sig, body);
        }

        public void GenerateCode(ICWriter writer) => throw new InvalidOperationException();
    }

    public record FunctionDeclaration : IDeclaration {
        private readonly FunctionSignature signature;
        private readonly ISyntax body;

        public TokenLocation Location { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, ISyntax body) {
            this.Location = loc;
            this.signature = sig;
            this.body = body;
        }

        public void DeclareNames(INamesRecorder names) { }

        public void DeclareTypes(ITypesRecorder paths) { }

        public IDeclaration CheckTypes(ITypesRecorder types) => this;

        public void GenerateCode(ICWriter writer) {
            var returnType = this.signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.signature.ReturnType);

            var pars = this.signature
                .Parameters
                .Select((x, i) => new CParameter() { 
                    Type = writer.ConvertType(x.Type),
                    Name = writer.GetVariableName(this.signature.Path.Append(x.Name))
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.signature.Path);
            var body = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, body);
            var retExpr = this.body.GenerateCode(bodyWriter);

            if (this.signature.ReturnType != PrimitiveType.Void) {
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

            writer.WriteDeclaration3(decl);
            writer.WriteDeclaration3(new CEmptyLine());

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration2(new CEmptyLine());
        }
    }
}
