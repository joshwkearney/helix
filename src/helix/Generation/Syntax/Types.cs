namespace Helix.Generation.Syntax {
    public record CNamedType(string Name) : ICSyntax {
        public string WriteToC() => this.Name;
    }

    public record CPointerType(ICSyntax InnerType) : ICSyntax {
        public string WriteToC() => this.InnerType.WriteToC() + "*";
    }
}
