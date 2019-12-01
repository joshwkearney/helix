using System;

namespace JoshuaKearney.Attempt15.Compiling {
    public class AnalyzeEventArgs : EventArgs {
        public Scope Context { get; }

        public TypeUnifier Unifier { get; }

        public AnalyzeEventArgs(TypeUnifier unifier, Scope context) {
            this.Unifier = unifier;
            this.Context = context;
        }

        public AnalyzeEventArgs SetContext(Scope scope) {
            return new AnalyzeEventArgs(this.Unifier, scope);
        }
    }
}