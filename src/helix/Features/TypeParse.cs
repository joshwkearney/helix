using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Features {
    public record TypeParse : IParseSyntax {
        private readonly HelixType type;

        public TokenLocation Location { get; }

        public IEnumerable<IParseSyntax> Children => Enumerable.Empty<IParseSyntax>();

        public bool IsPure => true;

        public TypeParse(TokenLocation loc, HelixType type) {
            this.Location = loc;
            this.type = type;
        }

        public Option<HelixType> AsType(TypeFrame types) => this.type;

        public ISyntax CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }
    }
}
