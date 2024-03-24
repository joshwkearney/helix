namespace Helix.MiddleEnd.Interpreting {
    public abstract record IValueLocation {
        public abstract bool IsUnknown { get; }

        public abstract T Accept<T>(IValueLocationVisitor<T> visitor);
    }
}
