using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class VoidToPrimitiveAdapter : ISyntax {
        public TokenLocation Location => this.Target.Location;

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public ISyntax Target { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            this.Target.GenerateCode(declWriter, statWriter);

            return CExpression.IntLiteral(0);
        }
    }

    public class BoolToIntAdapter : ISyntax {
        public TokenLocation Location => this.Target.Location;

        public TrophyType ReturnType => TrophyType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public ISyntax Target { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
