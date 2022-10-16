using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
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
            FunctionsHelper. CheckForDuplicateParameters(
                this.Location, 
                this.Signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareSignatureNames(this.Signature, scope, names);
        }

        public void DeclareTypes(IdentifierPath scope, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, types);

            FunctionsHelper.DeclareSignatureTypes(sig, scope, types);
        }

        public IDeclarationTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, types);
            var body = this.Body;

            // If this function returns void, wrap the body so we don't get weird type errors
            if (sig.ReturnType == PrimitiveType.Void) {
                body = new BlockSyntax(this.Body.Location, new ISyntaxTree[] { 
                    body, new VoidLiteral(body.Location)
                });
            }

            // Make sure the body is an rvalue
            if (!body.ResolveTypes(sig.Path, types).ToRValue(types).TryGetValue(out body)) {
                throw TypeCheckingErrors.RValueRequired(this.Body.Location);
            }

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
                .Select((x, i) => new CParameter(
                    writer.ConvertType(x.Type), 
                    writer.GetVariableName(this.Signature.Path.Append(x.Name))))
                .ToArray();

            var stats = new List<CStatement>();
            var bodyWriter = new CStatementWriter(writer, stats);
            var retExpr = this.Body.GenerateCode(types, bodyWriter);

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteSpacingLine();
                bodyWriter.WriteStatement(CStatement.Return(retExpr));
            }

            var funcName = writer.GetVariableName(this.Signature.Path);
            CDeclaration decl;
            CDeclaration forwardDecl;

            if (this.Signature.ReturnType == PrimitiveType.Void) {
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
