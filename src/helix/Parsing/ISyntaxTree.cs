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

        public Option<HelixType> AsType(EvalFrame types) => Option.None;

        public ISyntaxTree CheckTypes(EvalFrame types);

        // Mixins
        public void AnalyzeFlow(FlowFrame flow) {
            throw new Exception("Compiler bug");
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            throw new Exception("Compiler bug");
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            throw TypeCheckingErrors.RValueRequired(this.Location);
        }

        /// <summary>
        /// An LValue is a special type of syntax tree that is used to represent
        /// a location where values can be stored. LValues return and generate 
        /// pointer types but have lifetimes that match the inner type of the
        /// pointer. This is done so as to not rely on C's lvalue semantics. The
        /// lifetimes of an lvalue represent the region where the memory storage
        /// has been allocated, which any assigned values must outlive
        /// </summary>
        public ISyntaxTree ToLValue(EvalFrame types) {
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

        public void GenerateCode(FlowFrame types, ICWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
