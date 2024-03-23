namespace Helix.Common.Types {
    public abstract record IHelixType {
        public bool IsVoid => this is VoidType;

        public bool IsWord => this is WordType;

        public bool IsBool => this is BoolType;

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

    public record SingularBoolType(bool Value) : IHelixType {
        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitSingularBoolType(this);
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

    public record SingularUnionType : IHelixType {
        public IHelixType UnionType { get; }

        public string Member { get; }

        public IHelixType Value { get; }

        public SingularUnionType(IHelixType sig, string mem, IHelixType value) {
            this.UnionType = sig;
            this.Member = mem;
            this.Value = value;
        }

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitSingularUnionType(this);
    }

    public record VoidType : IHelixType {
        public static VoidType Instance { get; } = new VoidType();

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitVoidType(this);
    }

    public record WordType : IHelixType {
        public static WordType Instance { get; } = new WordType();

        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitWordType(this);
    }

    public record SingularWordType(long Value) : IHelixType {
        public override T Accept<T>(ITypeVisitor<T> visitor) => visitor.VisitSingularWordType(this);
    }
}