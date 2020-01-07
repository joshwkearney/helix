namespace Attempt17.Compiling {
    public class CompilerResult {
        public string HeaderText { get; }

        public string SourceText { get; }

        public CompilerResult(string headerText, string sourceText) {
            this.HeaderText = headerText;
            this.SourceText = sourceText;
        }
    }
}