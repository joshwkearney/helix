using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public interface ILValue : ISyntaxTree {
        public bool IsLocalVariable { get; }
    }

    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public bool IsPure { get; }

        public Option<HelixType> AsType(EvalFrame types) => Option.None;

        public ISyntaxTree CheckTypes(EvalFrame types);

        // Mixins
        public void AnalyzeFlow(FlowFrame flow) {
            throw new Exception("Compiler bug");
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            throw new Exception("Compiler bug");
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        public ILValue ToLValue(EvalFrame types) {
            throw TypeCheckingErrors.LValueRequired(this.Location);
        }

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

        public void AnalyzeFlow(FlowFrame flow) {
            throw new InvalidOperationException();
        }

        public void GenerateCode(EvalFrame types, ICWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
