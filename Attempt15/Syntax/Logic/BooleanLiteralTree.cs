using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Logic {
    public class BooleanLiteralTree : IParseTree, ISyntaxTree {
        public bool Value { get; }

        public ITrophyType ExpressionType => new SimpleType(TrophyTypeKind.Boolean);

        public ExternalVariablesCollection ExternalVariables => new ExternalVariablesCollection();

        public ISyntaxTree Analyze(AnalyzeEventArgs args) => this;

        public BooleanLiteralTree(bool value) {
            this.Value = value;
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            return this.Value ? "1" : "0";
        }

        public bool DoesVariableEscape(string variableName) => false;
    }
}
