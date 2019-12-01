using Attempt16.Analysis;

namespace Attempt16.Types {
    public interface ILanguageType {
        bool Equals(object other);

        T Accept<T>(ITypeVisitor<T> visitor);
    }
}