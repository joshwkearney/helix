using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers.Arrays {
    public class FixedArrayToArrayAdapter : ISyntax {
        public ISyntax Target { get; set; }

        public TokenLocation Location => this.Target.Location;

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
