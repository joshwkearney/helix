using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public interface ILValue : ISyntaxTree {
        public bool IsLocal { get; }
    }

    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public bool IsPure { get; }

        public Option<HelixType> AsType(EvalFrame types) => Option.None;

        public ISyntaxTree CheckTypes(EvalFrame types);

        public ISyntaxTree ToRValue(EvalFrame types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        public ILValue ToLValue(EvalFrame types) {
            throw TypeCheckingErrors.LValueRequired(this.Location);
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer);

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
    }

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(EvalFrame names);

        public void DeclareTypes(EvalFrame types);

        public IDeclaration CheckTypes(EvalFrame types);

        public void GenerateCode(EvalFrame types, ICWriter writer);
    }
}
