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

            public IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax, LanguageType type) {
                foreach (var unifier in registry.unifiers) {
                    var opt = unifier(syntax, type);

                    if (opt.Any()) {
                        return opt;
                    }
                }

                return Option.None<ISyntax<TypeCheckTag>>();
            }
        }
    }
}