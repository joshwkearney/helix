using Attempt18.Features;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt18.TypeChecking {
    public interface ITypeChecker {
        IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax, ITypeCheckScope scope, LanguageType type);
    }
}