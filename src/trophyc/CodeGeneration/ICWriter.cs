using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;

namespace Trophy {
    public interface ICWriter {
        public void WriteDeclaration3(CDeclaration decl);

        public void WriteDeclaration2(CDeclaration decl);

        public void WriteDeclaration1(CDeclaration decl);

        public CType ConvertType(ITrophyType type);
    }
}