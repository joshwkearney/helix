using Trophy.Analysis;
using Trophy.CodeGeneration;

namespace Trophy.Parsing {
    public interface IDeclarationTree {
        public TokenLocation Location { get; }

        public void DeclareNames(INamesRecorder names);

        public void DeclarePaths(ITypesRecorder paths);

        public IDeclarationTree CheckTypes(ITypesRecorder types);

        public void GenerateCode(CWriter writer);
    }
}