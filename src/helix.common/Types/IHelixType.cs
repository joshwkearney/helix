using Helix.Common.Collections;

namespace Helix.Common.Types {
    public abstract record IHelixType {
        public bool IsVoid => this is VoidType;

        public bool IsWord => this is WordType;

        public bool IsBool => this is BoolType;

        public Option<UnionType> AsUnion() => (this is UnionType type) ? type : Option.None;

        public abstract T Accept<T>(ITypeVisitor<T> visitor);

        public sealed override string ToString() => this.Accept(TypeStringifier.Instance);
    }

    public record ArrayType : IHelixType {
        public required IHelixType InnerType { get; set; }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitArrayType(this);
    }

    public record BoolType : IHelixType {
        public static BoolType Instance { get; } = new BoolType();

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitBoolType(this);
    }

    public record SingularBoolType : IHelixType {
        public required bool Value { get; init; }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitSingularBoolType(this);
    }

    public record FunctionType : IHelixType {
        public required IHelixType ReturnType { get; init; }

        public required IReadOnlyList<FunctionParameter> Parameters { get; init; }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitFunctionType(this);
    }

    public record FunctionParameter {
        public required string Name { get; init; }

        public required IHelixType Type { get; init; }

        public required bool IsMutable { get; init; }

        public override string ToString() {
            return (this.IsMutable ? "var" : "let") + " " + this.Name + " as " + this.Type.ToString();
        }
    }

    public record NominalType : IHelixType {
        public required string Name { get; init; }

        public required string DisplayName { get; init; }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitNominalType(this);
    }

    public record PointerType : IHelixType {
        public required IHelixType InnerType { get; init; }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitPointerType(this);
    }

    public record StructType : IHelixType {
        public ValueList<StructMember> Members { get; init; } = [];

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitStructType(this);
    }

    public record StructMember {
        public required string Name { get; init; }

        public required IHelixType Type { get; init; }

        public required bool IsMutable { get; init; }
    }

    public record UnionType : IHelixType {
        public ValueList<UnionMember> Members { get; init; } = [];

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitUnionType(this);
    }

    public record UnionMember {
        public required string Name { get; init; }

        public required IHelixType Type { get; init; }

        public required bool IsMutable { get; init; }
    }

    public record VoidType : IHelixType {
        public static VoidType Instance { get; } = new VoidType();

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitVoidType(this);
    }

    public record WordType : IHelixType {
        public static WordType Instance { get; } = new WordType();

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitWordType(this);
    }

    public record SingularWordType : IHelixType {
        public required long Value { get; init; }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitSingularWordType(this);
    }
}