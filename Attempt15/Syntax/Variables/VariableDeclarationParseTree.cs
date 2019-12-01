using JoshuaKearney.Attempt15.Compiling;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class VariableDeclarationParseTree : IParseTree {
        public string VariableName { get; }

        public IParseTree Assignment { get; }

        public IParseTree Appendix { get; }

        public bool IsImmutable { get; }

        public VariableDeclarationParseTree(string name, IParseTree assign, bool isImmutable, IParseTree appendix) {
            this.VariableName = name;
            this.Assignment = assign;
            this.Appendix = appendix;
            this.IsImmutable = isImmutable;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var assign = this.Assignment.Analyze(args);
            var newScope = args.Context.SetVariable(this.VariableName, new VariableInfo(this.VariableName, assign.ExpressionType, this.IsImmutable));
            var appendix = this.Appendix.Analyze(args.SetContext(newScope));

            return new VariableDeclarationSyntaxTree(
                name:        this.VariableName,
                assign:      assign,
                isImmutable: this.IsImmutable,
                appendix:    appendix
            );
        }
    }
}
