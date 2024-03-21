using Helix.Common.Hmm;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd {
    internal class AnalysisContext {
        public Stack<HmmWriter> WriterStack { get; } = [];

        public Stack<AnalysisScope> ScopeStack { get; } = [];

        public Stack<ControlFlowFrame> ControlFlowStack { get; } = [];

        public HmmWriter Writer => WriterStack.Peek();

        public ControlFlowFrame ControlFlow => this.ControlFlowStack.Peek();

        public TypeStore Types => this.ScopeStack.Peek().Types;

        public AliasStore Aliases => this.ScopeStack.Peek().Aliases;

        public PredicateStore Predicates => this.ScopeStack.Peek().Predicates;

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
            this.ScopeStack.Push(new AnalysisScope(this));
        }
    }
}
