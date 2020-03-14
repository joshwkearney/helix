using System;

namespace Attempt17 {
    public class IdentifierTargetVisitor<T> : IIdentifierTargetVisitor<T> {
        public Func<CompositeInfo, T> HandleComposite { get; set; }

        public Func<FunctionInfo, T> HandleFunction { get; set; }

        public Func<VariableInfo, T> HandleVariable { get; set; }

        public Func<ReservedIdentifier, T> HandleReserved { get; set; }

        public T VisitComposite(CompositeInfo composite) {
            if (this.HandleComposite == null) {
                throw new InvalidOperationException();
            }

            return this.HandleComposite(composite);
        }

        public T VisitFunction(FunctionInfo function) {
            if (this.HandleComposite == null) {
                throw new InvalidOperationException();
            }

            return this.HandleFunction(function);
        }

        public T VisitReserved(ReservedIdentifier reserved) {
            if (this.HandleComposite == null) {
                throw new InvalidOperationException();
            }

            return this.HandleReserved(reserved);
        }

        public T VisitVariable(VariableInfo variable) {
            if (this.HandleComposite == null) {
                throw new InvalidOperationException();
            }

            return this.HandleVariable(variable);
        }
    }
}