using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public interface ITypeChecker {
        ISyntax<TypeCheckTag> Check(ISyntax<ParseTag> syntax, Scope scope);

        bool IsTypeDefined(LanguageType type, Scope scope);
    }
}