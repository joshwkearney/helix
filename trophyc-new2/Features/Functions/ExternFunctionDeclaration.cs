using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private IDeclaration ExternFunctionDeclaration() {
            var start = this.Advance(TokenKind.ExternKeyword);
            var sig = this.FunctionSignature();
            var end = this.Advance(TokenKind.Semicolon);        
            var loc = start.Location.Span(end.Location);

            return new ExternFunctionParseDeclaration(loc, sig);
        }
    }
}

namespace Trophy.Features.Functions {
    public record ExternFunctionParseDeclaration : IDeclaration {
        public TokenLocation Location { get; }

        public FunctionParseSignature Signature { get; }

        public ExternFunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig) {
            this.Location = loc;
            this.Signature = sig;
        }

        public void DeclareNames(SyntaxFrame names) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location, 
                this.Signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareName(this.Signature, names);
        }

        public void DeclareTypes(SyntaxFrame types) {
            var sig = this.Signature.ResolveNames(types);
            var decl = new ExternFunctionDeclaration(this.Location, sig);

            // Replace the temporary wrapper object with a full declaration
            types.Trees[sig.Path] = new TypeSyntax(this.Location, new NamedType(sig.Path));

            // Declare this function
            types.Functions[sig.Path] = sig;
        }

        public IDeclaration CheckTypes(SyntaxFrame types) {
            var path = types.ResolvePath(this.Location.Scope, this.Signature.Name);
            var sig = types.Functions[path];

            return new ExternFunctionDeclaration(this.Location, sig);
        }

        public void GenerateCode(ICWriter writer) => throw new InvalidOperationException();
    }

    public record ExternFunctionDeclaration : IDeclaration {
        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public ExternFunctionDeclaration(TokenLocation loc, FunctionSignature sig) {
            this.Location = loc;
            this.Signature = sig;
        }

        public void DeclareNames(SyntaxFrame names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public IDeclaration CheckTypes(SyntaxFrame types) => this;

        public void GenerateCode(ICWriter writer) {
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

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);
        }
    }
}
