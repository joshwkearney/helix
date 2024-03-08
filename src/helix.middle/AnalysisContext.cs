using Helix.Common.Hmm;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd {
    internal class AnalysisContext {
        public Stack<HmmWriter> WriterStack { get; } = [];

        public Stack<TypeStore> TypesStack { get; } = [];

        public Stack<AliasingTracker> AliasesStack { get; } = [];

        public HmmWriter Writer => WriterStack.Peek();

        public TypeStore Types => TypesStack.Peek();

        public AliasingTracker Aliases => AliasesStack.Peek();

        public NamesStore Names { get; }

        public TypeChecker TypeChecker { get; }

        public TypeUnifier Unifier { get; }

        public AnalysisContext() {
            Names = new NamesStore();
            TypeChecker = new TypeChecker(this);
            Unifier = new TypeUnifier(this);

            WriterStack.Push(new HmmWriter());
            TypesStack.Push(new TypeStore(this));
            AliasesStack.Push(new AliasingTracker(this));
        }
    }
}
