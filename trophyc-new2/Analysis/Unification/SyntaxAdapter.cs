using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
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

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            this.original.GenerateCode(types, writer);

            return this.adapted.GenerateCode(types, writer);
        }
    }
}
