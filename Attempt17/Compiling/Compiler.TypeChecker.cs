using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private class TypeChecker : ITypeChecker {
            private readonly SyntaxRegistry registry;

            public TypeChecker(SyntaxRegistry registry, Scope scope) {
                this.registry = registry;
            }

            public ISyntax<TypeCheckTag> Check(ISyntax<ParseTag> syntax, Scope scope) {
                return this.registry.parseTrees[syntax.GetType()](syntax, scope, this);
            }

            public bool IsTypeDefined(LanguageType type, Scope scope) {
                return type.Accept(new TypeDefinitionChecker(scope));
            }
        }
    }
}