using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;
using static System.Formats.Asn1.AsnWriter;

namespace Trophy.Parsing {
    public partial class Parser {
        private FunctionParseSignature FunctionSignature() {
            this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<ParseFunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parName = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.AsKeyword);

                var parType = this.TypeExpression();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parName, parType));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.AsKeyword);

            var returnType = this.TypeExpression();
            var sig = new FunctionParseSignature(funcName, returnType, pars);

            return sig;
        }

        private IParseDeclaration FunctionDeclaration() {
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
    public class FunctionParseDeclaration : IParseDeclaration {
        public TokenLocation Location { get; }

        public FunctionParseSignature Signature { get; }

        public IParseTree Body { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, IParseTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.Body = body;
        }

        public void DeclareNames(IdentifierPath scope, NamesRecorder names) {
            CheckForDuplicateParameters(this.Location, this.Signature.Parameters.Select(x => x.Name));

            // Declare this function
            names.PutName(scope, this.Signature.Name, NameTarget.Function);

            // Declare the parameters
            foreach (var par in this.Signature.Parameters) {
                names.PutName(scope.Append(this.Signature.Name), par.Name, NameTarget.Variable);
            }
        }

        public void DeclareTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, names);

            // Declare this function
            types.Functions[sig.Path] = sig;
            types.Variables[sig.Path] = new FunctionType(sig);

            // Declare the parameters
            foreach (var par in sig.Parameters) {
                var path = sig.Path.Append(par.Name);

                types.Variables[path] = par.Type;
            }
        }

        public IDeclaration ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, names);
            var body = this.Body.ResolveTypes(sig.Path, names, types);

            // Make sure the return type matches the body's type
            if (body.TryUnifyTo(sig.ReturnType).TryGetValue(out var newBody)) {
                body = newBody;
            }
            else { 
                throw TypeCheckingErrors.UnexpectedType(this.Location, sig.ReturnType, body.ReturnType);
            }

            return new FunctionDeclaration(this.Location, sig, body);
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

    public class FunctionDeclaration : IDeclaration {
        public TokenLocation Location { get; }

        public FunctionSignature Signature { get; }

        public ISyntaxTree Body { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, ISyntaxTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.Body = body;
        }

        public void GenerateCode(CWriter writer) {
            var returnType = writer.ConvertType(this.Signature.ReturnType);
            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter(writer.ConvertType(x.Type), this.Signature.Path.Append(x.Name).ToCName()))
                .ToArray();

            var stats = new List<CStatement>();
            var bodyWriter = new CStatementWriter(writer, stats);
            var retExpr = this.Body.GenerateCode(writer, bodyWriter);

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
