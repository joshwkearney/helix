using System.Collections.Immutable;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class FunctionBoxSyntaxTree : ISyntaxTree {
        public ISyntaxTree Operand { get; }

        public FunctionInterfaceType ExpressionType { get; }

        ITrophyType ISyntaxTree.ExpressionType => this.ExpressionType;

        public ExternalVariablesCollection ExternalVariables => this.Operand.ExternalVariables;

        public FunctionBoxSyntaxTree(ISyntaxTree operand, FunctionInterfaceType target) {
            this.Operand = operand;
            this.ExpressionType = target;
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            return args.FunctionGenerator.GenerateFunctionInterfaceInstantiation(this, args);
        }

        public bool DoesVariableEscape(string variableName) {
            return this.Operand.DoesVariableEscape(variableName);
        }
    }
}