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
            var returnType = new VoidLiteral(end.Location) as ISyntaxTree;

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                returnType = this.TopExpression();
            }

            var loc = start.Location.Span(returnType.Location);
            var sig = new FunctionParseSignature(loc, funcName, returnType, pars);

            return sig;
        }

        private IDeclarationTree FunctionDeclaration() {
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
    public record FunctionParseDeclaration : IDeclarationTree {
        private readonly FunctionParseSignature signature;
        private readonly ISyntaxTree body;

        public TokenLocation Location { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, ISyntaxTree body) {
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

        public IDeclarationTree CheckTypes(ITypesRecorder types) {
            var path = types.CurrentScope.Append(this.signature.Name);
            var sig = types.GetFunction(path);
            var body = this.body;

            // If this function returns void, wrap the body so we don't get weird type errors
            if (sig.ReturnType == PrimitiveType.Void) {
                body = new BlockSyntax(this.body.Location, new ISyntaxTree[] {
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

    public record FunctionDeclaration : IDeclarationTree {
        private readonly FunctionSignature signature;
        private readonly ISyntaxTree body;

        public TokenLocation Location { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, ISyntaxTree body) {
            this.Location = loc;
            this.signature = sig;
            this.body = body;
        }

        public void DeclareNames(INamesRecorder names) { }

        public void DeclareTypes(ITypesRecorder paths) { }

        public IDeclarationTree CheckTypes(ITypesRecorder types) => this;

        public void GenerateCode(ICWriter writer) {
            var returnType = writer.ConvertType(this.signature.ReturnType);
            var pars = this.signature
                .Parameters
                .Select((x, i) => new CParameter(
                    writer.ConvertType(x.Type),
                    writer.GetVariableName(this.signature.Path.Append(x.Name))))
                .ToArray();

            var stats = new List<CStatement>();
            var bodyWriter = new CStatementWriter(writer, stats);
            var retExpr = this.body.GenerateCode(bodyWriter);

            if (this.signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteEmptyLine();
                bodyWriter.WriteStatement(CStatement.Return(retExpr));
            }

            var funcName = writer.GetVariableName(this.signature.Path);
            CDeclaration decl;
            CDeclaration forwardDecl;

            if (this.signature.ReturnType == PrimitiveType.Void) {
                decl = CDeclaration.Function(funcName, false, pars, stats);
                forwardDecl = CDeclaration.FunctionPrototype(funcName, false, pars);
            }
            else {
                decl = CDeclaration.Function(returnType, funcName, false, pars, stats);
                forwardDecl = CDeclaration.FunctionPrototype(returnType, funcName, false, pars);
            }

            writer.WriteDeclaration3(decl);
            writer.WriteDeclaration3(CDeclaration.EmptyLine());

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration2(CDeclaration.EmptyLine());
        }
    }
}
