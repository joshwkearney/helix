using System.Collections.Generic;

namespace Attempt3 {
    public class FunctionCall : IValue {
        public IValue Target { get; }

        public IReadOnlyList<IValue> Parameters { get; }

        public FunctionCall(IValue target, params IValue[] pars) {
            this.Target = target;
            this.Parameters = pars;
        }

        public void Accept(IValueVisitor visitor) {
            visitor.Visit(this);
        }

        public bool IsInvokable(IReadOnlyList<ILanguageType> types) {
            return false;
        }
    }
}