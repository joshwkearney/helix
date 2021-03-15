using System;

namespace Trophy.Analysis.Types {
    public enum TypeCopiability {
        Unconditional, Conditional
    }

    public interface ITrophyType : IEquatable<ITrophyType> {
        public static ITrophyType Boolean { get; } = new BoolType();

        public static ITrophyType Integer { get; } = new IntType();

        public static ITrophyType Void { get; } = new VoidType();

        public bool Equals(object other);

        public int GetHashCode();

        public string ToString();

        public TypeCopiability GetCopiability(ITypeRecorder types);

        public bool HasDefaultValue(ITypeRecorder types);

        public bool IsBoolType => false;

        public bool IsIntType => false;

        public bool IsVoidType => false;

        public IOption<ArrayType> AsArrayType() => Option.None<ArrayType>();

        public IOption<FixedArrayType> AsFixedArrayType() => Option.None<FixedArrayType>();

        public IOption<VarRefType> AsVariableType() => Option.None<VarRefType>();

        public IOption<SingularFunctionType> AsSingularFunctionType() => Option.None<SingularFunctionType>();

        public IOption<IdentifierPath> AsNamedType() => Option.None<IdentifierPath>();

        public IOption<FunctionType> AsFunctionType() => Option.None<FunctionType>();

        public IOption<GenericType> AsGenericType() => Option.None<GenericType>();
    }
}