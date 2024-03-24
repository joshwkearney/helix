namespace Helix.MiddleEnd.Interpreting {
    public record MemberAccessLocation : IValueLocation {
        public required IValueLocation Parent { get; init; }

        public required string Member { get; init; }

        public override bool IsUnknown => this.Parent.IsUnknown;

        public override T Accept<T>(IValueLocationVisitor<T> visitor) => visitor.VisitMemberAccess(this);

        public sealed override string ToString() => this.Parent.ToString() + "." + this.Member;
    }
}
