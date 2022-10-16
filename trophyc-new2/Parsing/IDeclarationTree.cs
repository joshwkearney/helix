using Trophy.Analysis;
using Trophy.CodeGeneration;

namespace Trophy.Parsing {
    public interface IDeclarationTree {
        public TokenLocation Location { get; }

        public void DeclareNames(IdentifierPath scope, TypesRecorder types);

        public void DeclareTypes(IdentifierPath scope, TypesRecorder types);

        public IDeclarationTree ResolveTypes(IdentifierPath scope, TypesRecorder types);

        public void GenerateCode(TypesRecorder types, CWriter writer);
    }
}