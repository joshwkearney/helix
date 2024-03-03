using Helix.Common.Hmm;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.Unification;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeCheckingContext {
        public Stack<HmmWriter> WriterStack { get; } = [];

        public HmmWriter Writer => this.WriterStack.Peek();

        public TypeStore Types { get; }

        public TypeCheckingNamesStore Names { get; }

        public TypeChecker TypeChecker { get; }

        public TypeUnifier Unifier { get; }

        public TypeCheckingContext() {
            this.Types = new TypeStore();
            this.Names = new TypeCheckingNamesStore();
            this.TypeChecker = new TypeChecker(this);
            this.Unifier = new TypeUnifier(this);

            this.WriterStack.Push(new HmmWriter());
        }
    }
}
