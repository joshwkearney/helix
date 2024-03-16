using Helix.Common.Hmm;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd {
    internal class AnalysisContext {
        public Stack<HmmWriter> WriterStack { get; } = [];

        public Stack<TypeStore> TypesStack { get; } = [];

        public Stack<AliasStore> AliasesStack { get; } = [];

        public Stack<ControlFlowFrame> ControlFlowStack { get; } = [];

        public HmmWriter Writer => WriterStack.Peek();

        public TypeStore Types => TypesStack.Peek();

        public AliasStore Aliases => AliasesStack.Peek();

        public NamesStore Names { get; }

        public TypeChecker TypeChecker { get; }

        public TypeUnifier Unifier { get; }

        public AliasTracker AliasTracker { get; }

        public AnalysisContext() {
            this.Names = new NamesStore();
            this.TypeChecker = new TypeChecker(this);
            this.Unifier = new TypeUnifier(this);
            this.AliasTracker = new AliasTracker(this);

            this.WriterStack.Push(new HmmWriter());
            this.TypesStack.Push(new TypeStore(this));
            this.AliasesStack.Push(new AliasStore(this));
        }
    }
}
