using System;
using System.Collections.Immutable;
using Attempt18.Features;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt18.TypeChecking {
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

        public static ISyntax<TypeCheckTag> UnifyTo(this ISyntax<TypeCheckTag> syntax,
            LanguageType type, TokenLocation loc, ITypeCheckScope scope) {

            return syntax.Tag.ReturnType.Accept(new TypeUnifier(syntax, type, scope, loc));
        }
    }
}
