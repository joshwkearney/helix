using System;
using System.Linq;

namespace Attempt20 {
    public enum TypeCopiability {
        Unconditional, Conditional
    }

    public abstract class LanguageType {
        public static LanguageType Boolean { get; } = new BoolType();
        public static LanguageType Integer { get; } = new IntType();
        public static LanguageType Void { get; } = new VoidType();

        public static LanguageType FromPath(IdentifierPath sig) {
            return new NamedType() { SignaturePath = sig };
        }

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

        public virtual IOption<VariableType> AsVariableType() { return Option.None<VariableType>(); }

        public virtual IOption<SingularFunctionType> AsSingularFunctionType() { return Option.None<SingularFunctionType>(); }

        public virtual IOption<IdentifierPath> AsNamedType() { return Option.None<IdentifierPath>(); }

        public static bool operator ==(LanguageType type1, LanguageType type2) {
            return type1.Equals(type2);
        }

        public static bool operator !=(LanguageType type1, LanguageType type2) {
            return !type1.Equals(type2);        
        }

        private class NamedType : LanguageType {
            public IdentifierPath SignaturePath { get; set; }

            public override IOption<IdentifierPath> AsNamedType() {
                return Option.Some(this.SignaturePath);
            }

            public override bool Equals(object other) {
                return other is NamedType type && this.SignaturePath == type.SignaturePath;
            }

            public override bool HasDefaultValue(ITypeRecorder types) {
                if (types.TryGetFunction(this.SignaturePath).Any()) {
                    return true;
                }
                else if (types.TryGetStruct(this.SignaturePath).TryGetValue(out var structSig)) {
                    return structSig.Members.All(x => x.MemberType.HasDefaultValue(types));
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public override TypeCopiability GetCopiability(ITypeRecorder types) {
                if (types.TryGetFunction(this.SignaturePath).Any()) {
                    return TypeCopiability.Unconditional;
                }
                else if (types.TryGetStruct(this.SignaturePath).TryGetValue(out var structSig)) {
                    if (structSig.Members.All(x => x.MemberType.GetCopiability(types) == TypeCopiability.Unconditional)) {
                        return TypeCopiability.Unconditional;
                    }
                    else {
                        return TypeCopiability.Conditional;
                    }
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public override int GetHashCode() {
                return this.SignaturePath.GetHashCode();
            }

            public override string ToString() {
                return this.SignaturePath.ToString();
            }
        }

        private class BoolType : LanguageType {
            public override bool IsBoolType => true;

            public BoolType() { }

            public override bool Equals(object other) => other is BoolType;

            public override int GetHashCode() => 11;

            public override string ToString() => "bool";

            public override bool HasDefaultValue(ITypeRecorder types) => true;

            public override TypeCopiability GetCopiability(ITypeRecorder types) {
                return TypeCopiability.Unconditional;
            }
        }

        private class IntType : LanguageType {
            public override bool IsIntType => true;

            public IntType() { }

            public override bool Equals(object other) => other is IntType;

            public override int GetHashCode() => 7;

            public override string ToString() => "int";

            public override bool HasDefaultValue(ITypeRecorder types) => true;

            public override TypeCopiability GetCopiability(ITypeRecorder types) {
                return TypeCopiability.Unconditional;
            }
        }

        private class VoidType : LanguageType {
            public override bool IsVoidType => true;

            public VoidType() { }

            public override bool Equals(object other) => other is VoidType;

            public override int GetHashCode() => 5;

            public override string ToString() => "void";

            public override bool HasDefaultValue(ITypeRecorder types) => true;

            public override TypeCopiability GetCopiability(ITypeRecorder types) {
                return TypeCopiability.Unconditional;
            }
        }
    }

    public class SingularFunctionType : LanguageType {
        public IdentifierPath FunctionPath { get; }

        public SingularFunctionType(IdentifierPath path) {
            this.FunctionPath = path;
        }

        public override bool Equals(object other) {
            return other is SingularFunctionType type && this.FunctionPath == type.FunctionPath;
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override int GetHashCode() {
            return this.FunctionPath.GetHashCode();
        }

        public override string ToString() {
            return this.FunctionPath.Segments.Last();
        }

        public override IOption<SingularFunctionType> AsSingularFunctionType() {
            return Option.Some(this);
        }
    }

    public class ArrayType : LanguageType {
        public LanguageType ElementType { get; }

        public ArrayType(LanguageType elemType) {
            this.ElementType = elemType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is ArrayType arrType) {
                return this.ElementType == arrType.ElementType;
            }

            return false;
        }

        public override int GetHashCode() {
            return ElementType.GetHashCode();
        }

        public override string ToString() {
            return this.ElementType.ToString() + "[]";
        }

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override IOption<ArrayType> AsArrayType() {
            return Option.Some(this);
        }
    }

    public class FixedArrayType : LanguageType {
        public int Size { get; }

        public LanguageType ElementType { get; }

        public FixedArrayType(LanguageType elemType, int size) {
            this.ElementType = elemType;
            this.Size = size;
        }

        public override bool Equals(object obj) {
            return obj is FixedArrayType other && this.ElementType.Equals(other.ElementType) && this.Size == other.Size; 
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override int GetHashCode() {
            return this.Size + 7 * this.ElementType.GetHashCode();
        }

        public override bool HasDefaultValue(ITypeRecorder types) {
            return false;
        }

        public override string ToString() {
            return this.ElementType.ToString() + "[" + this.Size.ToString() + "]";
        }

        public override IOption<FixedArrayType> AsFixedArrayType() {
            return Option.Some(this);
        }
    }

    public class VariableType : LanguageType {
        public LanguageType InnerType { get; }

        public VariableType(LanguageType innerType) {
            this.InnerType = innerType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is VariableType varType) {
                return this.InnerType == varType.InnerType;
            }

            return false;
        }

        public override bool HasDefaultValue(ITypeRecorder types) => false;

        public override int GetHashCode() => 7 * this.InnerType.GetHashCode();

        public override string ToString() => "var " + this.InnerType.ToString();

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override IOption<VariableType> AsVariableType() {
            return Option.Some(this);
        }
    }
}