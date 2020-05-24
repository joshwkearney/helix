using System;
using System.Collections.Immutable;

namespace Attempt19.Types {
    public static class TypeExtensions {
        public static Copiability GetCopiability(this LanguageType type, TypeChache types) {
            return type.Accept(new TypeCopiabilityVisitor(types));
        }

        public static ImmutableHashSet<LanguageType> GetAccessors(this LanguageType type, TypeChache types) {
            return type.Accept(new TypeAccessorsVisitor(types));
        }

        public static ImmutableHashSet<LanguageType> GetMutators(this LanguageType type, TypeChache types) {
            return type.Accept(new TypeMutatorsVisitor(types));
        }

        public static LanguageType Resolve(this LanguageType type, NameCache<NameTarget> names) {
            return type.Accept(new TypeResolutionVisitor(names));
        }
    }
}