using Helix.Common.Hmm;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.Unification;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeCheckingContext {
        public Stack<HmmWriter> WriterStack { get; } = [];

        public Stack<TypeStore> TypesStack { get; } = [];

        public Stack<AliasingTracker> AliasesStack { get; } = [];

        public HmmWriter Writer => this.WriterStack.Peek();

        public TypeStore Types => this.TypesStack.Peek();

        public AliasingTracker Aliases => this.AliasesStack.Peek();

        public TypeCheckingNamesStore Names { get; }

        public TypeChecker TypeChecker { get; }

        public TypeUnifier Unifier { get; }

        public TypeCheckingContext() {
            this.Names = new TypeCheckingNamesStore();
            this.TypeChecker = new TypeChecker(this);
            this.Unifier = new TypeUnifier(this);

            this.WriterStack.Push(new HmmWriter());
            this.TypesStack.Push(new TypeStore());
            this.AliasesStack.Push(new AliasingTracker(this));
        }
    }
}
