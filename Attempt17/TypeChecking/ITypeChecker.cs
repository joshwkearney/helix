using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public interface ITypeChecker {
        ISyntax<TypeCheckTag> Check(ISyntax<ParseTag> syntax, IScope scope);

        bool IsTypeDefined(LanguageType type, IScope scope);

        TypeCopiability GetTypeCopiability(LanguageType type, IScope scope);

        IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax, LanguageType type);
    }
}