using Attempt17.CodeGeneration;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features {
    public delegate ISyntax<TypeCheckTag> SyntaxTypeChecker<T>(T syntax, IScope scope, ITypeChecker checker) where T : ISyntax<ParseTag>;

    public delegate CBlock SyntaxCodeGenerator<T>(T syntax, ICScope scope, ICodeGenerator gen) where T : ISyntax<TypeCheckTag>;

    public delegate void DeclarationScopeModifier<T>(T syntax, IScope scope) where T : ISyntax<ParseTag>;

    public interface ISyntaxRegistry {
        void RegisterParseTree<T>(SyntaxTypeChecker<T> typeChecker) where T : ISyntax<ParseTag>;

        void RegisterSyntaxTree<T>(SyntaxCodeGenerator<T> codeGen) where T : ISyntax<TypeCheckTag>;

        void RegisterDeclaration<T>(DeclarationScopeModifier<T> scopeModifier) where T : ISyntax<ParseTag>;
    }
}