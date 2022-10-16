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

        public void DeclareNames(IdentifierPath scope, TypesRecorder names) {
            FunctionsHelper.CheckForDuplicateParameters(
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

            return new ExternFunctionSignature(this.Location, sig);
        }

        public void GenerateCode(TypesRecorder types, CWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public class ExternFunctionSignature : IDeclarationTree {
        public TokenLocation Location { get; }

        public FunctionSignature Signature { get; }

        public ExternFunctionSignature(TokenLocation loc, FunctionSignature sig) {
            this.Location = loc;
            this.Signature = sig;
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

            var funcName = writer.GetVariableName(this.Signature.Path);
            var stats = new List<CStatement>();

            CDeclaration forwardDecl;

            if (this.Signature.ReturnType == PrimitiveType.Void) {
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
