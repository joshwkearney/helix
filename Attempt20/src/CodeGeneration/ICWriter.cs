using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;

namespace Attempt20 {
    public interface ICWriter {
        public void WriteDeclaration(CDeclaration decl);

        public void WriteForwardDeclaration(CDeclaration decl);

        public void RequireRegions();

        public CType ConvertType(TrophyType type);
    }
}