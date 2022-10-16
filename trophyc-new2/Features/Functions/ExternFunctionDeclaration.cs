using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private IDeclarationTree ExternFunctionDeclaration() {
            var start = this.Advance(TokenKind.ExternKeyword);
            var sig = this.FunctionSignature();
            var end = this.Advance(TokenKind.Semicolon);        
            var loc = start.Location.Span(end.Location);

            return new ExternFunctionParseSignature(loc, sig);
        }
    }
}

namespace Trophy.Features.Functions {
    public class ExternFunctionParseSignature : IDeclarationTree {
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

        public void DeclarePaths(ITypesRecorder types) {
            var sig = this.Signature.ResolveNames(types);

            FunctionsHelper.DeclareSignaturePaths(sig, types);
        }

        public IDeclarationTree CheckTypes(ITypesRecorder types) => this;

        public void GenerateCode(CWriter writer) {
            var path = writer.TryFindPath(this.Signature.Name).GetValue();
            var sig = writer.GetFunction(path);

            var returnType = writer.ConvertType(sig.ReturnType);
            var pars = sig
                .Parameters
                .Select((x, i) => new CParameter(
                    writer.ConvertType(x.Type),
                    writer.GetVariableName(sig.Path.Append(x.Name))))
                .ToArray();

            var funcName = writer.GetVariableName(sig.Path);
            var stats = new List<CStatement>();

            CDeclaration forwardDecl;

            if (sig.ReturnType == PrimitiveType.Void) {
                forwardDecl = CDeclaration.FunctionPrototype(funcName, false, pars);
            }
            else {
                forwardDecl = CDeclaration.FunctionPrototype(returnType, funcName, false, pars);
            }

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration2(CDeclaration.EmptyLine());
        }
    }
}
