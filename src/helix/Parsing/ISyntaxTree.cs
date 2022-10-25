using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public bool IsPure { get; }

        public Option<HelixType> AsType(SyntaxFrame types) => Option.None;

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
