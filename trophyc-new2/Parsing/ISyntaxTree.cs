using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Generation;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public bool IsPure { get; }

        public bool RewriteNonlocalFlow(SyntaxFrame types, FlowRewriter flow) => false;

        public Option<TrophyType> AsType(SyntaxFrame types) => Option.None;

        public ISyntaxTree CheckTypes(SyntaxFrame types);

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw TypeCheckingErrors.LValueRequired(this.Location);
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer);

        // Mixins
        public IEnumerable<ISyntaxTree> GetAllChildren() {
            var stack = new Queue<ISyntaxTree>(this.Children);

            while (stack.Count > 0) {
                var item = stack.Dequeue();

                foreach (var child in item.Children) {
                    stack.Enqueue(child);
                }

                yield return item;
            }
        }

        public bool HasNonlocalFlow() {
            foreach (var child in this.GetAllChildren()) {
                if (child is BreakContinueSyntax) {
                    return true;
                }
            }

            return false;
        }
    }

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(SyntaxFrame names);

        public void DeclareTypes(SyntaxFrame types);

        public IDeclaration CheckTypes(SyntaxFrame types);

        public void GenerateCode(SyntaxFrame types, ICWriter writer);
    }
}
