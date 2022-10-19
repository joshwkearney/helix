using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax TypePointer(ISyntax start) {
            TokenLocation loc;
            bool isWritable;

            if (this.Peek(TokenKind.Multiply)) {
                loc = this.Advance(TokenKind.Multiply).Location;
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

namespace Trophy.Features.Primitives {
    public record TypePointerSyntax : ISyntax {
        private readonly ISyntax inner;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public TypePointerSyntax(TokenLocation loc, ISyntax inner, bool isWritable) {
            this.Location = loc;
            this.inner = inner;
            this.isWritable = isWritable;
        }

        public Option<TrophyType> AsType(ITypesRecorder names) {
            return this.inner.AsType(names)
                .Select(x => new PointerType(x, this.isWritable))
                .Select(x => (TrophyType)x);
        }

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
