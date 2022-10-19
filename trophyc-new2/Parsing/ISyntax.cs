using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public interface ISyntax {
        public TokenLocation Location { get; }

        public Result<TrophyType> AsType(INamesRecorder names) {
            return new InvalidOperationException(
                "Compiler error: This syntax tree cannot be construed as a type");
        }

        public ISyntax CheckTypes(ITypesRecorder types);

        public ISyntax ToRValue(ITypesRecorder types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            throw TypeCheckingErrors.LValueRequired(this.Location);
        }

        public ICSyntax GenerateCode(ICStatementWriter writer);
    }

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(INamesRecorder names);

        public void DeclareTypes(ITypesRecorder types);

        public IDeclaration CheckTypes(ITypesRecorder types);

        public void GenerateCode(ICWriter writer);
    }
}
