using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Features.Functions;

namespace Trophy {
    public abstract class TrophyType : IEquatable<TrophyType> {
        public virtual Option<PointerType> AsPointerType() => new();

        public virtual Option<NamedType> AsNamedType() => new();

        public abstract bool Equals(TrophyType? other);

        public abstract override int GetHashCode();

        public override bool Equals(object? obj) {
            return obj is TrophyType type && this.Equals(type);
        }

        public static bool operator ==(TrophyType type1, TrophyType type2) {
            if (type1 is null) {
                return false;
            }

            return type1.Equals(type2);
        }

        public static bool operator !=(TrophyType type1, TrophyType type2) {
            if (type1 is null) {
                return false;
            }

            return !type1.Equals(type2);
        }
    }

    public class FunctionType : TrophyType {
        public FunctionSignature Signature { get; }

        public FunctionType(FunctionSignature sig) {
            this.Signature = sig;
        }

        public override bool Equals(TrophyType? other) {
            return other is FunctionType type && this.Signature == type.Signature;
        }

        public override int GetHashCode() {
            return this.Signature.GetHashCode();
        }

        public override string ToString() {
            return "func["
                 + this.Signature.ReturnType
                 + "," + String.Join(", ", this.Signature.Parameters.Select(x => x.Type))
                 + "]";
        }
    }

    public class PrimitiveType : TrophyType {
        private readonly PrimitiveTypeKind kind;

        public static PrimitiveType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);

        public static PrimitiveType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Bool);

        public static PrimitiveType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);

        public static PrimitiveType Void { get; } = new PrimitiveType(PrimitiveTypeKind.Void);

        private PrimitiveType(PrimitiveTypeKind kind) {
            this.kind = kind;
        }

        public override bool Equals(TrophyType? other) {
            return other is PrimitiveType type && type.kind == this.kind;
        }

        public override int GetHashCode() => (int)this.kind;

        public override string ToString() {
            return this.kind switch {
                PrimitiveTypeKind.Int   => "int",
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Bool  => "bool",
                PrimitiveTypeKind.Void  => "void",
                _                       => throw new Exception("Unexpected primitive type kind"),
            };
        }

        private enum PrimitiveTypeKind {
            Int = 11, 
            Float = 13, 
            Bool = 17,
            Void = 19,
        }
    }

    public class PointerType : TrophyType {
        public TrophyType ReferencedType { get; }

        public PointerType(TrophyType innerType) {
            this.ReferencedType = innerType;
        }

        public override Option<PointerType> AsPointerType() => this;

        public override bool Equals(TrophyType? other) {
            return other is PointerType type && type.ReferencedType.Equals(this.ReferencedType);
        }

        public override int GetHashCode() => 67 * this.ReferencedType.GetHashCode();

        public override string ToString() {
            return this.ReferencedType + "*";
        }
    }

    public class NamedType : TrophyType {
        public IdentifierPath FullName { get; } 

        public NamedType(IdentifierPath fullName) {
            this.FullName = fullName;
        }

        public override bool Equals(TrophyType? other) {
            return other is NamedType type && type.FullName == this.FullName;
        }

        public override Option<NamedType> AsNamedType() => this;

        public override int GetHashCode() => this.FullName.GetHashCode();

        public override string ToString() {
            return this.FullName.Segments.Last();
        }
    }
}