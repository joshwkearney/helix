using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Features.Arrays
{
    public record ArrayTypeSyntax : ISyntaxTree {
        private readonly ISyntaxTree inner;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children {
            get => new[] { this.inner };
        }

        public bool IsPure => this.inner.IsPure;

        public ArrayTypeSyntax(TokenLocation loc, ISyntaxTree inner) {
            this.Location = loc;
            this.inner = inner;
        }

        Option<HelixType> ISyntaxTree.AsType(TypeFrame types) {
            return this.inner
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;
    }
}
