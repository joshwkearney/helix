using Attempt17.NewSyntax;

namespace Attempt18.Types {
    public abstract class LanguageType {
        public abstract T Accept<T>(ITypeVisitor<T> visitor);

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract override string ToString();

        public abstract string ToFriendlyString();

        public abstract TypeCopiability GetCopiability();

        public static bool operator ==(LanguageType type1, LanguageType type2) {
            return type1.Equals(type2);
        }

        public static bool operator !=(LanguageType type1, LanguageType type2) {
            return !type1.Equals(type2);        
        }
    }
}