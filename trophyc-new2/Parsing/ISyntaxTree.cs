using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public Option<TrophyType> ToType(INamesObserver names, IdentifierPath currentScope);

        public ISyntaxTree CheckTypes(INamesObserver names, ITypesRecorder types);

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types);

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types);

        public CExpression GenerateCode(CStatementWriter writer);
    }
}
