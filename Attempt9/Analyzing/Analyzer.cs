using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt9 {
    public class Analyzer : IParseTreeVisitor {
        private readonly IParseTree tree;
        private int tempVariable = 0;

        private readonly Stack<List<IStatementSyntax>> blocks = new Stack<List<IStatementSyntax>>();
        private readonly Stack<IExpressionSyntax> values = new Stack<IExpressionSyntax>();
        private readonly Stack<Scope> scopes = new Stack<Scope>();

        public Analyzer(IParseTree tree) {
            this.tree = tree;
        }

        public IReadOnlyList<IStatementSyntax> Analyze() {
            this.blocks.Clear();
            this.blocks.Push(new List<IStatementSyntax>());

            this.values.Clear();

            this.scopes.Clear();
            this.scopes.Push(new Scope());

            this.tree.Accept(this);
            return this.blocks.Peek();
        }

        public void Visit(BinaryExpression value) {
            value.Left.Accept(this);
            value.Right.Accept(this);

            var right = this.values.Pop();
            var left = this.values.Pop();

            if (right.ReturnType != left.ReturnType) {
                throw new Exception();
            }

            SyntaxBinaryOperator op;
            ITrophyType type;

            if (left.ReturnType == PrimitiveTrophyType.Int64Type) {
                type = PrimitiveTrophyType.Int64Type;

                switch (value.Operator) {
                    case BinaryOperator.Add:
                        op = SyntaxBinaryOperator.Add;
                        break;
                    case BinaryOperator.Subtract:
                        op = SyntaxBinaryOperator.Subtract;
                        break;
                    case BinaryOperator.Multiply:
                        op = SyntaxBinaryOperator.Multiply;
                        break;
                    case BinaryOperator.Divide:
                        op = SyntaxBinaryOperator.Divide;
                        break;
                    default:
                        throw new Exception();
                }
            }            
            else {
                throw new Exception();
            }

            this.values.Push(new BinaryExpressionSyntax(op, left, right, type));
        }

        public void Visit(UnaryExpression value) {
            value.Expression.Accept(this);
            var expr = this.values.Pop();

            if (value.Operator == UnaryOperator.Box) {
                string temp = "$temp" + this.tempVariable++;
                var type = ReferenceType.GetReferenceType(expr.ReturnType);

                this.blocks.Peek().Add(new ReferenceCreateStatement(temp, expr));
                this.values.Push(new VariableLiteralSyntax(temp, type));

                return;
            }
            else if (value.Operator == UnaryOperator.Unbox) {
                if (!(expr.ReturnType is ReferenceType refType)) {
                    throw new Exception($"Cannot unbox value type '{expr.ReturnType}'");
                }

                this.values.Push(new UnaryExpressionSyntax(SyntaxUnaryOperator.Unbox, expr, refType.InnerType));
                return;
            }
            else {
                SyntaxUnaryOperator op;
                ITrophyType type;

                if (expr.ReturnType == PrimitiveTrophyType.Int64Type) {
                    type = PrimitiveTrophyType.Int64Type;

                    switch (value.Operator) {
                        case UnaryOperator.Negation:
                            op = SyntaxUnaryOperator.Negation;
                            break;
                        default:
                            throw new Exception();
                    }
                }
                else {
                    throw new Exception();
                }

                this.values.Push(new UnaryExpressionSyntax(op, expr, type));
            }
        }

        public void Visit(Int64Literal value) {
            this.values.Push(new Int64LiteralSyntax(value.Value));
        }

        public void Visit(VariableLiteral value) {
            if (!this.scopes.Peek().Variables.TryGetValue(value.Name, out var info)) {
                throw new Exception($"Variable '{value.Name}' not found");
            }

            //if (info.Scope.ClosureLevel != this.scopes.Peek().ClosureLevel) {
            //    throw new Exception($"Cannot close on '{value.Name}'");
            //}

            this.values.Push(new VariableLiteralSyntax(value.Name, info.Type));
        }

        public void Visit(IfExpression value) {
            // Get temp variable
            string temp = "$temp" + this.tempVariable++;

            // Convert condition
            value.Condition.Accept(this);
            var condition = this.values.Pop();

            //if (condition.ReturnType != PrimitiveTrophyType.Boolean) {
            //    throw new Exception("Conditions must be boolean expressions");
            //}

            // Convert affirmative            
            this.blocks.Push(new List<IStatementSyntax>());
            value.AffirmativeExpression.Accept(this);

            var affirmBlock = this.blocks.Pop();
            var affirm = this.values.Pop();

            // Convert negative
            this.blocks.Push(new List<IStatementSyntax>());
            value.NegativeExpression.Accept(this);

            var negative = this.values.Pop();
            var negativeBlock = this.blocks.Pop();

            // Type check
            if (affirm.ReturnType != negative.ReturnType) {
                throw new Exception();
            }

            // Push if statement
            this.blocks.Peek().Add(new VariableDeclarationSyntax(temp, affirm.ReturnType));
            affirmBlock.Add(new VariableAssignmentSyntax(temp, affirm));
            negativeBlock.Add(new VariableAssignmentSyntax(temp, negative));

            this.blocks.Peek().Add(new IfStatement(condition, affirmBlock, negativeBlock));
            this.values.Push(new VariableLiteralSyntax(temp, affirm.ReturnType));
        }

        public void Visit(VariableDefinition value) {
            if (this.scopes.Peek().Variables.ContainsKey(value.Name)) {
                throw new Exception($"Variable {value.Name} already defined");
            }

            value.AssignExpression.Accept(this);
            var assign = this.values.Pop();

            if (assign.ReturnType is ReferenceType) {
                this.blocks.Peek().Add(new ReferenceCheckinStatement(assign));
            }

            this.blocks.Peek().Add(new VariableDeclarationAssignmentSyntax(value.Name, assign));

            int closureLevel = this.scopes.Peek().ClosureLevel;
            var scope = this.scopes.Peek().AddVariable(value.Name, new VariableInfo(assign.ReturnType, closureLevel));
            this.scopes.Push(scope);

            value.ScopeExpression.Accept(this);

            this.scopes.Pop();

            if (assign.ReturnType is ReferenceType) {
                this.blocks.Peek().Add(new ReferenceCheckoutStatement(assign));
            }
        }
    }
}