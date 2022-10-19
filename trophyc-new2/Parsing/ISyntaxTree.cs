using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public Option<TrophyType> AsType(SyntaxFrame types) {
            throw new InvalidOperationException(
                "Compiler error: This syntax tree cannot be construed as a type");
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types);

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw TypeCheckingErrors.LValueRequired(this.Location);
        }

        public ICSyntax GenerateCode(ICStatementWriter writer);
    }

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(SyntaxFrame names);

        public void DeclareTypes(SyntaxFrame types);

        public IDeclaration CheckTypes(SyntaxFrame types);

        public void GenerateCode(ICWriter writer);
    }
}
