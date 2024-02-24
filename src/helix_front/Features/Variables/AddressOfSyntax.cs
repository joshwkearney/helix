using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Variables {
    public class AddressOfSyntax : IParseTree {
        private readonly IParseTree target;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { target };

        public bool IsPure => target.IsPure;

        public AddressOfSyntax(TokenLocation loc, IParseTree target) {
            Location = loc;
            this.target = target;
        }

        public IParseTree ToRValue(TypeFrame types) {
            return this;
        }
    }
}