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

            return new ExternFunctionParseSignature(loc, sig);
        }
    }
}

namespace Trophy.Features.Functions {
    public record ExternFunctionParseSignature : IDeclaration {
        public TokenLocation Location { get; }

        public FunctionParseSignature Signature { get; }

        public ExternFunctionParseSignature(TokenLocation loc, FunctionParseSignature sig) {
            this.Location = loc;
            this.Signature = sig;
        }

        public void DeclareNames(INamesRecorder names) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location, 
                this.Signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareSignatureNames(this.Signature, names);
        }

        public void DeclareTypes(ITypesRecorder types) {
            var sig = this.Signature.ResolveNames(types);

            FunctionsHelper.DeclareSignaturePaths(sig, types);
        }

        public IDeclaration CheckTypes(ITypesRecorder types) {
            var path = types.TryFindPath(this.Signature.Name).GetValue();
            var sig = types.GetFunction(path);

            return new ExternFunctionSignature(this.Location, sig);
        }

        public void GenerateCode(ICWriter writer) => throw new InvalidOperationException();
    }

    public record ExternFunctionSignature : IDeclaration {
        private readonly FunctionSignature signature;

        public TokenLocation Location { get; }

        public ExternFunctionSignature(TokenLocation loc, FunctionSignature sig) {
            this.Location = loc;
            this.signature = sig;
        }

        public void DeclareNames(INamesRecorder names) { }

        public void DeclareTypes(ITypesRecorder types) { }

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

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration2(new CEmptyLine());
        }
    }
}
