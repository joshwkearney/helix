namespace Helix.MiddleEnd.Interpreting {
    public record NamedLocation(string Name) : IValueLocation {
        public override bool IsUnknown => false;

        public override T Accept<T>(IValueLocationVisitor<T> visitor) => visitor.VisitLocal(this);

        public sealed override string ToString() => this.Name;
    }
}
