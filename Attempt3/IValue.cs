using System.Collections.Generic;

namespace Attempt3 {
    public interface IValue {
        bool IsInvokable(IReadOnlyList<ILanguageType> types);

        void Accept(IValueVisitor visitor);
    }
}