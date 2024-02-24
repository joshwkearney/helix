using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Analysis;

namespace Helix.Features.Arrays {
    public record ArrayTypeSyntax : IParseTree {
        private readonly IParseTree inner;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children {
            get => new[] { this.inner };
        }

        public bool IsPure => this.inner.IsPure;

        public ArrayTypeSyntax(TokenLocation loc, IParseTree inner) {
            this.Location = loc;
            this.inner = inner;
        }

        Option<HelixType> IParseTree.AsType(TypeFrame types) {
            return this.inner
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public IParseTree CheckTypes(TypeFrame types) => this;
    }
}
