using Attempt6.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Analyzing {
    public class SemanticAnalyzer : IProtoSyntaxVisitor {
        private readonly IProtoSyntax syntax;

        private readonly Stack<ISyntax> values = new Stack<ISyntax>();
        private readonly Stack<Scope> scopes = new Stack<Scope>();

        public SemanticAnalyzer(IProtoSyntax syntax) {
            this.syntax = syntax;
        }

        public ISyntax Analyze() {
            this.values.Clear();
            this.scopes.Push(new Scope());

            this.syntax.Accept(this);
            return this.values.Pop();
        }

        public void Visit(ProtoBinaryExpression syntax) {
            IFunctionSyntax func;

            switch (syntax.Operator) {
                case BinaryOperator.Add:
                    func = IntrinsicFunction.AddInt32;
                    break;
                case BinaryOperator.Subtract:
                    func = IntrinsicFunction.SubtractInt32;
                    break;
                case BinaryOperator.Multiply:
                    func = IntrinsicFunction.MultiplyInt32;
                    break;
                case BinaryOperator.Divide:
                    func = IntrinsicFunction.DivideInt32;
                    break;
                default:
                    throw new Exception("Unexpected binary operator");
            }

            syntax.LeftTarget.Accept(this);
            var left = this.values.Pop();

            syntax.RightTarget.Accept(this);
            var right = this.values.Pop();

            if (left.ExpressionType != PrimitiveType.Int32Type && right.ExpressionType != PrimitiveType.Int32Type) {
                throw new Exception();
            }

            this.values.Push(new FunctionCallExpression(func, new[] { left, right }));
        }

        public void Visit(Int32Literal syntax) {
            this.values.Push(syntax);
        }

        public void Visit(ProtoVariableDeclaration syntax) {
            // Get assignment tree
            syntax.AssignExpression.Accept(this);
            var assign = this.values.Pop();

            // Get a new variable location
            var location = new VariableLocation(syntax.Name, syntax.IsReadOnly, assign.ExpressionType);

            // Create a new scope with variable
            var nScope = this.scopes.Peek().AppendVariable(syntax.Name, location);

            // Push new scope
            this.scopes.Push(nScope);

            syntax.ScopeExpression.Accept(this);
            var scopeExpr = this.values.Pop();
            this.values.Push(new VariableAssignment(location, assign, scopeExpr, syntax.IsReadOnly));

            this.scopes.Pop();
        }

        public void Visit(ProtoStatement syntax) { 
            syntax.StatementExpression.Accept(this);
            var stat = this.values.Pop();

            syntax.ReturnExpression.Accept(this);
            var ret = this.values.Pop();

            this.values.Push(new Statement(stat, ret));
        }

        public void Visit(ProtoVariableLiteral syntax) {
            if (this.scopes.Peek().Variables.TryGetValue(syntax.VariableName, out var loc)) {
                this.values.Push(loc);
            }
            else {
                throw new Exception($"Undeclared variable {syntax.VariableName}");
            }
        }

        public void Visit(ProtoVariableStore syntax) {
            if (this.scopes.Peek().Variables.TryGetValue(syntax.VariableName, out var loc)) {
                if (loc.IsReadOnly) {
                    throw new Exception("Cannot store to a read only variable");
                }

                syntax.AssignExpression.Accept(this);
                var assign = this.values.Pop();

                if (assign.ExpressionType != loc.ExpressionType) {
                    throw new Exception($"Cannot store value of type '{assign.ExpressionType}' into {loc.ExpressionType} {loc.Name}");
                }

                this.values.Push(new VariableAssignment(loc, assign, loc, false));
            }
            else {
                throw new Exception($"Undeclared variable {syntax.VariableName}");
            }
        }
    }
}