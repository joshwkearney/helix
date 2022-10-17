using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Features.Primitives {
    public record IntSyntaxAdapter : ISyntax {
        private readonly ISyntax inner;

        public TokenLocation Location => this.inner.Location;

        public IntSyntaxAdapter(ISyntax inner) {
            this.inner = inner;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) {
            return Option.None;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            return this;
        }

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CCast() {
                Type = writer.ConvertType(PrimitiveType.Int),
                Target = this.inner.GenerateCode(writer)
            };
        }
    }
}
