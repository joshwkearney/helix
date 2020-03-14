using Attempt17.Features;
using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public interface ITypeChecker {
        IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax, ITypeCheckScope scope, LanguageType type);
    }
}