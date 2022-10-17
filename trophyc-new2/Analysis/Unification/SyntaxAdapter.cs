using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Analysis.Unification {
    public class SyntaxAdapter : ISyntax {
        private readonly ISyntax original;
        private readonly ISyntax adapted;

        public TokenLocation Location => this.original.Location;

        public SyntaxAdapter(ISyntax original, ISyntax adapted) {
            this.original = original;
            this.adapted = adapted;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) {
            return Option.None;
        }

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntax> ToLValue(ITypesRecorder types) => Option.None;

        public Option<ISyntax> ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            this.original.GenerateCode(writer);

            return this.adapted.GenerateCode(writer);
        }
    }
}
