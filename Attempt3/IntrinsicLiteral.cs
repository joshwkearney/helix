using System.Collections.Generic;

namespace Attempt3 {
    public class IntrinsicLiteral : IValue {
        public string Name { get; }

        public IntrinsicLiteral(string name) {
            this.Name = name;
        }

        public void Accept(IValueVisitor visitor) {
            visitor.Visit(this);
        }

        public bool IsInvokable(IReadOnlyList<ILanguageType> types) {
            return true;
        }
    }
}