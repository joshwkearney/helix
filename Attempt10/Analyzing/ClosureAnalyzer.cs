using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class ClosureAnalyzer : ISyntaxTreeVisitor {
        private readonly ISyntaxTree tree;
        private readonly Dictionary<string, VariableInfo> vars = new Dictionary<string, VariableInfo>();

        public ClosureAnalyzer(ISyntaxTree tree) {
            this.tree = tree;
        }

        public IReadOnlyList<VariableInfo> GetClosedVariables() {
            this.vars.Clear();
            this.tree.Accept(this);

            return this.vars.Values.ToArray();
        }

        public void Visit(Int64LiteralSyntax value) { }

        public void Visit(VariableLiteralSyntax value) {
            if (this.tree.Scope.Variables.TryGetValue(value.Name, out var info)) {
                int definedClosureLevel = this.tree.Scope.Variables[value.Name].DefinedClosureLevel;

                if (definedClosureLevel < tree.Scope.ClosureLevel) {
                    this.vars[value.Name] = info;
                }
            }
        }

        public void Visit(IfExpressionSyntax value) {
            value.Condition.Accept(this);
            value.AffirmativeExpression.Accept(this);
            value.NegativeExpression.Accept(this);
        }

        public void Visit(VariableDefinitionSyntax value) {
            value.AssignExpression.Accept(this);
            value.ScopeExpression.Accept(this);
        }

        public void Visit(FunctionLiteralSyntax value) {
            foreach (var closed in value.ClosedVariables) {
                if (closed.DefinedClosureLevel < tree.Scope.ClosureLevel) {
                    this.vars[closed.Name] = closed;
                }
            }
        }

        public void Visit(FunctionInvokeSyntax value) {
            value.Target.Accept(this);

            foreach (var arg in value.Arguments) {
                arg.Accept(this);
            }
        }

        public void Visit(BoolLiteralSyntax value) { }

        public void Visit(Real64Literal value) { }

        public void Visit(PrimitiveOperationSyntax value) {
            foreach (var arg in value.Operands) {
                arg.Accept(this);
            }
        }

        public void Visit(FunctionRecurseLiteral value) { }
    }
}