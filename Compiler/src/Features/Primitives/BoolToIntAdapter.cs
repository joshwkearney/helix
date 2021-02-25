using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Features.Primitives {
    public class BoolToIntAdapter : ISyntaxC {
        private readonly ISyntaxC target;

        public TrophyType ReturnType => TrophyType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public BoolToIntAdapter(ISyntaxC target) {
            this.target = target;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.target.GenerateCode(declWriter, statWriter);
        }
    }
}
