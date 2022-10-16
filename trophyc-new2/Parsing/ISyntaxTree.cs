using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types);

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types);

        public Option<ISyntaxTree> ToRValue(TypesRecorder types);

        public Option<ISyntaxTree> ToLValue(TypesRecorder types);

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter);
    }
}
