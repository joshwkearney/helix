using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Features.Primitives {
    public class VoidToPrimitiveAdapterC : ISyntaxC {
        private readonly ISyntaxC target;

        public ITrophyType ReturnType { get; }

        public VoidToPrimitiveAdapterC(ISyntaxC target, ITrophyType returnType) {
            this.target = target;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            this.target.GenerateCode(declWriter, statWriter);

            return CExpression.IntLiteral(0);
        }
    }
}
