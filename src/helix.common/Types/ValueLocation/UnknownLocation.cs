namespace Helix.MiddleEnd.Interpreting {
    public record UnknownLocation : IValueLocation {
        public static UnknownLocation Instance { get; } = new();

        public override bool IsUnknown => true;

        public override T Accept<T>(IValueLocationVisitor<T> visitor) => visitor.VisitUnknown(this);

        public sealed override string ToString() => "unknown";
    }
}
