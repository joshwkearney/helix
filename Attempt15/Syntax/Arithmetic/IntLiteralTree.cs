using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Arithmetic {
    public class IntLiteralTree : IParseTree, ISyntaxTree {
        public long Value { get; }

        public ITrophyType ExpressionType => new SimpleType(TrophyTypeKind.Int);

        public ExternalVariablesCollection ExternalVariables => new ExternalVariablesCollection();

        public IntLiteralTree(long value) {
            this.Value = value;
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            return this.Value.ToString();
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            return this;
        }

        public bool DoesVariableEscape(string variableName) => false;
    }
}
