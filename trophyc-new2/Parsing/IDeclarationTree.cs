using Trophy.Analysis;
using Trophy.CodeGeneration;

namespace Trophy.Parsing {
    public interface IDeclarationTree {
        public TokenLocation Location { get; }

        public void DeclareNames(INamesRecorder names);

        public void DeclarePaths(INamesObserver names, ITypesRecorder paths);

        public IDeclarationTree CheckTypes(INamesObserver names, ITypesRecorder types);

        public void GenerateCode(CWriter writer);
    }
}