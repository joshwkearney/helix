using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Features.Arrays {
    public record ArrayTypeParse : IParseSyntax {
        private readonly IParseSyntax inner;

        public TokenLocation Location { get; }

        public IEnumerable<IParseSyntax> Children {
            get => new[] { this.inner };
        }

        public bool IsPure => this.inner.IsPure;

        public ArrayTypeParse(TokenLocation loc, IParseSyntax inner) {
            this.Location = loc;
            this.inner = inner;
        }

        Option<HelixType> IParseSyntax.AsType(TypeFrame types) {
            return this.inner
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public IParseSyntax CheckTypes(TypeFrame types) => this;
    }
}
