using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Variables {
    public class CompoundSyntax : IParseTree {
        private readonly IReadOnlyList<IParseTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => this.args;

        public bool IsPure { get; }

        public CompoundSyntax(TokenLocation loc, IReadOnlyList<IParseTree> args) {
            this.Location = loc;
            this.args = args;

            this.IsPure = args.All(x => x.IsPure);
        }

        public IParseTree ToRValue(TypeFrame types) => this;
    }
}
