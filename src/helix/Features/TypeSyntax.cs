using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Features {
    public record TypeSyntax : ISyntaxTree {
        private readonly HelixType type;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public TypeSyntax(TokenLocation loc, HelixType type) {
            this.Location = loc;
            this.type = type;
        }

        public Option<HelixType> AsType(TypeFrame types) => this.type;

        public ISyntaxTree CheckTypes(TypeFrame types) => this;
    }
}
