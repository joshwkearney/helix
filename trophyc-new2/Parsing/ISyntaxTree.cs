using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public Option<TrophyType> AsType(SyntaxFrame types) => Option.None;

        public ISyntaxTree CheckTypes(SyntaxFrame types);

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw TypeCheckingErrors.LValueRequired(this.Location);
        }

        public ICSyntax GenerateCode(ICStatementWriter writer);

        // Mixins
        public IEnumerable<ISyntaxTree> GetAllChildren() {
            var stack = new Stack<ISyntaxTree>(this.Children);

            while (stack.Count > 0) {
                var item = stack.Pop();

                foreach (var child in item.Children) {
                    stack.Push(child);
                }

                yield return item;
            }
        }
    }

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(SyntaxFrame names);

        public void DeclareTypes(SyntaxFrame types);

        public IDeclaration CheckTypes(SyntaxFrame types);

        public void GenerateCode(ICWriter writer);
    }
}
