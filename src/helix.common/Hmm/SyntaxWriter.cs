using System.Collections.Immutable;

namespace Helix.Common.Hmm {
    public class SyntaxWriter<T> {
        private readonly List<T> lines = [];
        private readonly List<T> typeDeclarations = [];
        private readonly List<T> forwardDeclarations = [];

        public IReadOnlyList<T> ScopedLines => this.lines;

        public IReadOnlyList<T> AllLines {
            get => [.. this.typeDeclarations, .. this.forwardDeclarations, .. this.lines];
        }

        private SyntaxWriter(List<T> typeDeclarations, List<T> forwardDeclarations) {
            this.typeDeclarations = typeDeclarations;
            this.forwardDeclarations = forwardDeclarations;
        }

        public SyntaxWriter() { }

        public void AddLine(T line) {
            this.lines.Add(line);
        }

        public void AddTypeDeclaration(T syntax) {
            this.typeDeclarations.Add(syntax);
        }

        public void AddFowardDeclaration(T syntax) {
            this.forwardDeclarations.Add(syntax);
        }

        public SyntaxWriter<T> CreateScope() => new(this.typeDeclarations, this.forwardDeclarations);
    }
}
