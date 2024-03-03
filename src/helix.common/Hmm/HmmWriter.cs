using System.Collections.Immutable;

namespace Helix.Common.Hmm {
    public class HmmWriter {
        private List<IHmmSyntax> lines = [];
        private List<IHmmSyntax> typeDeclarations = [];
        private List<IHmmSyntax> forwardDeclarations = [];

        public IReadOnlyList<IHmmSyntax> ScopedLines => this.lines;

        public IReadOnlyList<IHmmSyntax> AllLines {
            get => [.. this.typeDeclarations, .. this.forwardDeclarations, .. this.lines];
        }

        private HmmWriter(List<IHmmSyntax> typeDeclarations, List<IHmmSyntax> forwardDeclarations) {
            this.typeDeclarations = typeDeclarations;
            this.forwardDeclarations = forwardDeclarations;
        }

        public HmmWriter() { }

        public void AddLine(IHmmSyntax line) {
            this.lines.Add(line);
        }

        public void AddTypeDeclaration(IHmmSyntax syntax) {
            this.typeDeclarations.Add(syntax);
        }

        public void AddFowardDeclaration(IHmmSyntax syntax) {
            this.forwardDeclarations.Add(syntax);
        }

        public HmmWriter CreateScope() => new HmmWriter(this.typeDeclarations, this.forwardDeclarations);
    }
}
