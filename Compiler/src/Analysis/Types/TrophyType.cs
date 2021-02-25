using Compiler.Analysis.Types;

namespace Attempt20.Analysis.Types {
    public enum TypeCopiability {
        Unconditional, Conditional
    }

    public abstract class TrophyType {
        public static TrophyType Boolean { get; } = new BoolType();
        public static TrophyType Integer { get; } = new IntType();
        public static TrophyType Void { get; } = new VoidType();

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract override string ToString();

        public abstract TypeCopiability GetCopiability(ITypeRecorder types);

        public abstract bool HasDefaultValue(ITypeRecorder types);

        public virtual bool IsBoolType => false;

        public virtual bool IsIntType => false;

        public virtual bool IsVoidType => false;

        public virtual IOption<ArrayType> AsArrayType() { return Option.None<ArrayType>(); }

        public virtual IOption<FixedArrayType> AsFixedArrayType() { return Option.None<FixedArrayType>(); }

        public virtual IOption<VarRefType> AsVariableType() { return Option.None<VarRefType>(); }

        public virtual IOption<SingularFunctionType> AsSingularFunctionType() { return Option.None<SingularFunctionType>(); }

        public virtual IOption<IdentifierPath> AsNamedType() { return Option.None<IdentifierPath>(); }

        public virtual IOption<FunctionType> AsFunctionType() { return Option.None<FunctionType>(); }

        public static bool operator ==(TrophyType type1, TrophyType type2) {
            return type1.Equals(type2);
        }

        public static bool operator !=(TrophyType type1, TrophyType type2) {
            return !type1.Equals(type2);        
        }
    }
}