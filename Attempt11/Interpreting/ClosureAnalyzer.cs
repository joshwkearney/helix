using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public class ClosureAnalyzer : ISyntaxTreeVisitor {
        private readonly ISyntaxTree tree;
        private readonly HashSet<string> definedVariables = new HashSet<string>();
        private readonly Dictionary<string, VariableInfo> closedVars = new Dictionary<string, VariableInfo>();

        public ClosureAnalyzer(ISyntaxTree tree) {
            this.tree = tree;
        }

        public IReadOnlyList<VariableInfo> Analyze() {
            this.closedVars.Clear();
            this.tree.Accept(this);

            return this.closedVars.Values.ToArray();
        }

        public void Visit(BinaryExpressionSyntax value) {
            value.Left.Accept(this);
            value.Right.Accept(this);
        }

        public void Visit(UnaryExpressionSyntax value) {
            value.Operand.Accept(this);
        }

        public void Visit(Int64LiteralSyntax value) { }

        public void Visit(VariableLiteralSyntax value) {
            if (!this.definedVariables.Contains(value.Name)) {
                this.closedVars.Add(value.Name, new VariableInfo(value.Name, value.ExpressionType));
            }
        }

        public void Visit(IfExpressionSyntax value) {
            value.Condition.Accept(this);
            value.AffirmativeExpression.Accept(this);
            value.NegativeExpression.Accept(this);
        }

        public void Visit(VariableDefinitionSyntax value) {
            value.AssignExpression.Accept(this);

            this.definedVariables.Add(value.Name);
            value.ScopeExpression.Accept(this);
        }

        public void Visit(FunctionLiteralSyntax value) {
            value.Body.Accept(this);
        }

        public void Visit(FunctionInvokeSyntax value) {
            value.Target.Accept(this);

            foreach (var arg in value.Arguments) {
                arg.Accept(this);
            }
        }

        public void Visit(BoolLiteralSyntax value) { }

        public void Visit(Real64Literal value) { }
    }
}