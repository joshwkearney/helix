using System.Collections.Immutable;

namespace Attempt19.Types {
    public enum Copiability {
        Unconditional, Conditional, None
    }

    public enum LanguageTypeKind {
        Array, Bool, Int, Unresolved, Variable, Void, Function, Struct
    }

    public interface ITypeVisitor<T> {
        T VisitArrayType(ArrayType type);

        T VisitBoolType(BoolType type);

        T VisitFunctionType(FunctionType type);

        T VisitIntType(IntType type);

        T VisitStructType(StructType type);

        T VisitUnresolvedType(UnresolvedType type);

        T VisitVariableType(VariableType type);

        T VisitVoidType(VoidType type);
    }

    public abstract class LanguageType {
        public abstract LanguageTypeKind Kind { get; }

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract override string ToString();

        public abstract T Accept<T>(ITypeVisitor<T> visitor);

        public static bool operator ==(LanguageType type1, LanguageType type2) {
            return type1.Equals(type2);
        }

        public static bool operator !=(LanguageType type1, LanguageType type2) {
            return !type1.Equals(type2);        
        }
    }
}