using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;

namespace Trophy.Features.Primitives {
    public class BoolToIntAdapter : ISyntaxC {
        private readonly ISyntaxC target;

        public ITrophyType ReturnType => ITrophyType.Integer;

        public BoolToIntAdapter(ISyntaxC target) {
            this.target = target;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.target.GenerateCode(declWriter, statWriter);
        }
    }
}
