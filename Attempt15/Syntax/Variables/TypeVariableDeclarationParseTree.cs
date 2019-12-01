using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Parsing;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class TypeVariableDeclarationParseTree : IParseTree {
        public string VariableName { get; }

        public TypePotential Assignment { get; }

        public IParseTree Appendix { get; }

        public TypeVariableDeclarationParseTree(string name, TypePotential assign, IParseTree appendix) {
            this.VariableName = name;
            this.Assignment = assign;
            this.Appendix = appendix;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var scope = args.Context.SetType(this.VariableName, this.Assignment(args.Context));
            return this.Appendix.Analyze(args.SetContext(scope));
        }
    }
}