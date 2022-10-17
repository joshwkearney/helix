using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree TypePointer(ISyntaxTree start) {
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
    public record TypePointerSyntax : ISyntaxTree {
        private readonly ISyntaxTree inner;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public TypePointerSyntax(TokenLocation loc, ISyntaxTree inner, bool isWritable) {
            this.Location = loc;
            this.inner = inner;
            this.isWritable = isWritable;
        }

        public Option<TrophyType> ToType(INamesRecorder names) {
            return this.inner.ToType(names)
                .Select(x => new PointerType(x, this.isWritable))
                .Select(x => (TrophyType)x);
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
