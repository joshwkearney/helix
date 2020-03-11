using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public interface ITypeChecker {
        ISyntax<TypeCheckTag> Check(ISyntax<ParseTag> syntax, ITypeCheckScope scope);

        bool IsTypeDefined(LanguageType type, ITypeCheckScope scope);

        TypeCopiability GetTypeCopiability(LanguageType type, ITypeCheckScope scope);

        IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax, ITypeCheckScope scope, LanguageType type);
    }
}