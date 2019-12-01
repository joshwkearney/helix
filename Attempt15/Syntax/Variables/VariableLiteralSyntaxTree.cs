using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class VariableLiteralSyntaxTree : ISyntaxTree {
        public string VariableName { get; }

        public ITrophyType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        public VariableLiteralSyntaxTree(string name, ITrophyType type, bool isImmutable) {
            this.VariableName = name;
            this.ExpressionType = type;
            this.ExternalVariables = new ExternalVariablesCollection()
                .SetVariableInfo(new VariableInfo(this.VariableName, this.ExpressionType, isImmutable));
        } 

        public string GenerateCode(CodeGenerateEventArgs args) {
            return this.VariableName;
        }

        public bool DoesVariableEscape(string variableName) {
            return this.VariableName == variableName;
        }
    }
}
