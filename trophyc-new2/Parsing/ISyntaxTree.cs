using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public Option<TrophyType> ToType(INamesRecorder names);

        public ISyntaxTree CheckTypes(ITypesRecorder types);

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types);

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types);

        public CExpression GenerateCode(CStatementWriter writer);
    }

    public interface IDeclarationTree {
        public TokenLocation Location { get; }

        public void DeclareNames(INamesRecorder names);

        public void DeclareTypes(ITypesRecorder types);

        public IDeclarationTree CheckTypes(ITypesRecorder types);

        public void GenerateCode(CWriter writer);
    }
}
