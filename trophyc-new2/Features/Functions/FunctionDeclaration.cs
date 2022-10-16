using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
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

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.AsKeyword);

            var returnType = this.TopExpression();
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
    public class FunctionParseDeclaration : IDeclarationTree {
        public TokenLocation Location { get; }

        public FunctionParseSignature Signature { get; }

        public ISyntaxTree Body { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, ISyntaxTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.Body = body;
        }

        public void DeclareNames(IdentifierPath scope, TypesRecorder names) {
            CheckForDuplicateParameters(this.Location, this.Signature.Parameters.Select(x => x.Name));

            // Declare this function
            if (!names.TrySetNameTarget(scope, this.Signature.Name, NameTarget.Function)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Signature.Location, this.Signature.Name);
            }

            // Declare the parameters
            foreach (var par in this.Signature.Parameters) {
                var path = scope.Append(this.Signature.Name);

                if (!names.TrySetNameTarget(path, par.Name, NameTarget.Variable)) {
                    throw TypeCheckingErrors.IdentifierDefined(par.Location, par.Name);
                }
            }
        }

        public void DeclareTypes(IdentifierPath scope, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, types);

            // Declare this function
            types.SetFunction(sig);
            types.SetVariable(sig.Path, new FunctionType(sig), false);

            // Declare the parameters
            for (int i = 0; i < this.Signature.Parameters.Count; i++) {
                var parsePar = this.Signature.Parameters[i];
                var type = sig.Parameters[i].Type;
                var path = sig.Path.Append(parsePar.Name);

                types.SetVariable(path, type, parsePar.IsWritable);
            }
        }

        public IDeclarationTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, types);
            var body = this.Body.ResolveTypes(sig.Path, types);
            var bodyType = types.GetReturnType(body);

            // Make sure the return type matches the body's type
            if (TypeUnifier.TryUnifyTo(body, bodyType, sig.ReturnType).TryGetValue(out var newBody)) {
                body = newBody;
            }
            else { 
                throw TypeCheckingErrors.UnexpectedType(this.Location, sig.ReturnType, bodyType);
            }

            return new FunctionDeclaration(this.Location, sig, body);
        }

        public void GenerateCode(TypesRecorder types, CWriter writer) {
            throw new InvalidOperationException();
        }

        private static void CheckForDuplicateParameters(TokenLocation loc, IEnumerable<string> pars) {
            var dups = pars
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(loc, dups.First());
            }
        }
    }

    public class FunctionDeclaration : IDeclarationTree {
        public TokenLocation Location { get; }

        public FunctionSignature Signature { get; }

        public ISyntaxTree Body { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, ISyntaxTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.Body = body;
        }

        public void DeclareNames(IdentifierPath scope, TypesRecorder types) { }

        public void DeclareTypes(IdentifierPath scope, TypesRecorder types) { }

        public IDeclarationTree ResolveTypes(IdentifierPath scope, TypesRecorder types) => this;

        public void GenerateCode(TypesRecorder types, CWriter writer) {
            var returnType = writer.ConvertType(this.Signature.ReturnType);
            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter(writer.ConvertType(x.Type), this.Signature.Path.Append(x.Name).ToCName()))
                .ToArray();

            var stats = new List<CStatement>();
            var bodyWriter = new CStatementWriter(writer, stats);
            var retExpr = this.Body.GenerateCode(types, bodyWriter);

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteSpacingLine();
                bodyWriter.WriteStatement(CStatement.Return(retExpr));
            }

            CDeclaration decl;
            CDeclaration forwardDecl;

            if (this.Signature.ReturnType == PrimitiveType.Void) {
                decl = CDeclaration.Function(this.Signature.Path.ToCName(), false, pars, stats);
                forwardDecl = CDeclaration.FunctionPrototype(this.Signature.Path.ToCName(), false, pars);
            }
            else {
                decl = CDeclaration.Function(returnType, this.Signature.Path.ToCName(), false, pars, stats);
                forwardDecl = CDeclaration.FunctionPrototype(returnType, this.Signature.Path.ToCName(), false, pars);
            }

            writer.WriteDeclaration3(decl);
            writer.WriteDeclaration3(CDeclaration.EmptyLine());

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration2(CDeclaration.EmptyLine());
        }

    }
}
