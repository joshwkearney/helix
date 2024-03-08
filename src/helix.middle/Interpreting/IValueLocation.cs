namespace Helix.MiddleEnd.Interpreting {
    public abstract record IValueLocation {
        public abstract bool IsUnknown { get; }

        public abstract T Accept<T>(ILValueVisitor<T> visitor);
    }

    public interface ILValueVisitor<T> {
        public T VisitUnknown(UnknownLocation lvalue);

        public T VisitLocal(NamedLocation lvalue);

        public T VisitMemberAccess(MemberAccessLocation lvalue);
    }

    public record UnknownLocation : IValueLocation {
        public static UnknownLocation Instance { get; } = new();

        public override bool IsUnknown => true;

        public override T Accept<T>(ILValueVisitor<T> visitor) => visitor.VisitUnknown(this);

        public sealed override string ToString() => "unknown";
    }

    public record NamedLocation(string Name) : IValueLocation {
        public override bool IsUnknown => false;

        public override T Accept<T>(ILValueVisitor<T> visitor) => visitor.VisitLocal(this);

        public sealed override string ToString() => this.Name;
    }

    public record MemberAccessLocation : IValueLocation {
        public required IValueLocation Parent { get; init; }

        public required string Member { get; init; }

        public override bool IsUnknown => this.Parent.IsUnknown;

        public override T Accept<T>(ILValueVisitor<T> visitor) => visitor.VisitMemberAccess(this);

        public sealed override string ToString() => this.Parent.ToString() + "." + this.Member;
    }
}
