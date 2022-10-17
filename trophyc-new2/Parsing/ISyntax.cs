using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public interface ISyntax {
        public TokenLocation Location { get; }

        public Option<TrophyType> ToType(INamesRecorder names);

        public ISyntax CheckTypes(ITypesRecorder types);

        public Option<ISyntax> ToRValue(ITypesRecorder types);

        public Option<ISyntax> ToLValue(ITypesRecorder types);

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
