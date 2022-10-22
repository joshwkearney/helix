using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree TypePointer(ISyntaxTree start, BlockBuilder block) {
            TokenLocation loc;
            bool isWritable;

            if (this.Peek(TokenKind.Star)) {
                loc = this.Advance(TokenKind.Star).Location;
                isWritable = true;
            }
            else {
                loc = this.Advance(TokenKind.Caret).Location;
                isWritable = false;
            }

            loc = start.Location.Span(loc);

            return new TypePointerSyntax(loc, start, isWritable);
        }
    }
}

namespace Helix.Features.Primitives {
    public record TypePointerSyntax : ISyntaxTree {
        private readonly ISyntaxTree inner;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.inner };

        public bool IsPure => this.inner.IsPure;

        public TypePointerSyntax(TokenLocation loc, ISyntaxTree inner, bool isWritable) {
            this.Location = loc;
            this.inner = inner;
            this.isWritable = isWritable;
        }

        public Option<HelixType> AsType(SyntaxFrame types) {
            return this.inner.AsType(types)
                .Select(x => new PointerType(x, this.isWritable))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
