using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Parsing;

namespace Trophy.Analysis.Unification {
    public class SyntaxAdapter : ISyntaxTree {
        private readonly ISyntaxTree original;
        private readonly ISyntaxTree adapted;

        public TokenLocation Location => this.original.Location;

        public SyntaxAdapter(ISyntaxTree original, ISyntaxTree adapted) {
            this.original = original;
            this.adapted = adapted;
        }

        public Option<TrophyType> ToType(INamesRecorder names) {
            return Option.None;
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public CExpression GenerateCode(ICStatementWriter writer) {
            this.original.GenerateCode(writer);

            return this.adapted.GenerateCode(writer);
        }
    }
}
