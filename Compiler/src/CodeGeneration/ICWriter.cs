using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy {
    public interface ICWriter {
        public void WriteDeclaration(CDeclaration decl);

        public void WriteForwardDeclaration(CDeclaration decl);

        public void RequireRegions();

        public CType ConvertType(TrophyType type);
    }
}