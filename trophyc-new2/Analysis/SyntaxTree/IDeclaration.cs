using Trophy.CodeGeneration;

namespace Trophy.Analysis.SyntaxTree {
    public interface IDeclaration {
        public void GenerateCode(CWriter writer);
    }
}