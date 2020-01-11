using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private class TypeChecker : ITypeChecker {
            private readonly SyntaxRegistry registry;

            public TypeChecker(SyntaxRegistry registry, IScope scope) {
                this.registry = registry;
            }

            public ISyntax<TypeCheckTag> Check(ISyntax<ParseTag> syntax, IScope scope) {
                return this.registry.parseTrees[syntax.GetType()](syntax, scope, this);
            }

            public TypeCopiability GetTypeCopiability(LanguageType type, IScope scope) {
                return type.Accept(new TypeCopiabilityVisitor(scope));
            }

            public bool IsTypeDefined(LanguageType type, IScope scope) {
                return type.Accept(new TypeDefinitionVisitor(scope));
            }
        }
    }
}