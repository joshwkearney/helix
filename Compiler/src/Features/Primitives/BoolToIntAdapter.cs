using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;

namespace Attempt20.Features.Primitives {
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
