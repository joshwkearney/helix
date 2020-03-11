using Attempt17.CodeGeneration;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features {
    public delegate ISyntax<TypeCheckTag> SyntaxTypeChecker<T>(T syntax, ITypeCheckScope scope, ITypeChecker checker) where T : ISyntax<ParseTag>;

    public delegate CBlock SyntaxCodeGenerator<T>(T syntax, ICScope scope, ICodeGenerator gen) where T : ISyntax<TypeCheckTag>;

    public delegate void DeclarationScopeModifier<T>(T syntax, ITypeCheckScope scope) where T : ISyntax<ParseTag>;

    public delegate IOption<ISyntax<TypeCheckTag>> TypeUnifier(ISyntax<TypeCheckTag> syntax, ITypeCheckScope scope, LanguageType type);

    public interface ISyntaxRegistry {
        void RegisterParseTree<T>(SyntaxTypeChecker<T> typeChecker) where T : ISyntax<ParseTag>;

        void RegisterSyntaxTree<T>(SyntaxCodeGenerator<T> codeGen) where T : ISyntax<TypeCheckTag>;

        void RegisterDeclaration<T>(DeclarationScopeModifier<T> scopeModifier) where T : ISyntax<ParseTag>;

        void RegisterTypeUnifier(TypeUnifier unifier);
    }
}