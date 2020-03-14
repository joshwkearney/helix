using System;
using System.Collections.Immutable;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public static class TypeExtensions {
        public static bool IsDefined(this LanguageType type, ITypeCheckScope scope) {
            return type.Accept(new TypeDefinitionVisitor(scope));
        }

        public static bool IsMovable(this LanguageType type, ITypeCheckScope scope) {
            return type.Accept(new TypeMovabilityVisitor(scope));
        }

        public static ImmutableHashSet<LanguageType> GetMutators(this LanguageType type, ITypeCheckScope scope) {
            return type.Accept(new TypeMutatorsVisitor(scope));
        }

        public static ImmutableHashSet<LanguageType> GetAccessibleTypes(this LanguageType type, ITypeCheckScope scope) {
            return type.Accept(new AccessibleTypesVisitor(scope));
        }

        public static bool IsCopiable(this LanguageType type, IScope scope) {
            return type.Accept(new TypeMovabilityVisitor(scope));
        }

        public static bool IsCircular(this LanguageType type, ITypeCheckScope scope) {
            return type.Accept(new CircularValueObjectDetector(type, scope));
        }

        public static TypeCopiability GetCopiability(this LanguageType type, ITypeCheckScope scope) {
            return type.Accept(new TypeCopiabilityVisitor(scope));
        }
    }
}
