using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public class Analyzer {
        private readonly Stack<Scope> scopes = new Stack<Scope>();

        public Analyzer() {
            this.scopes.Push(new Scope());
        }

        public ISyntaxTree AnalyzeInvoke(ISyntaxTree target, IReadOnlyList<ISyntaxTree> args) {
            if (!(target.ExpressionType is ClosureTrophyType funcType)) {
                throw new Exception();
            }

            if (funcType.ParameterTypes.Count != args.Count) {
                throw new Exception();
            }

            foreach (var (t1, t2) in args.Zip(funcType.ParameterTypes, (x, y) => (x.ExpressionType, y))) {
                if (t1 != t2) {
                    throw new Exception();
                }
            }

            return new FunctionInvokeSyntax(target, funcType.ReturnType, this.scopes.Peek(), args);
        }

        public ISyntaxTree AnalyzeFunctionLiteral(IReadOnlyList<string> argNames, IReadOnlyList<ITrophyType> argTypes, Func<ISyntaxTree> bodyFactory) {
            if (argNames.Count != argTypes.Count) {
                throw new Exception();
            }

            foreach (var name in argNames) {
                if (this.scopes.Peek().Variables.ContainsKey(name)) {
                    throw new Exception("Function arguments cannot shadow local variables");
                }
            }

            var scope = this.scopes.Peek();
            var args = new List<FunctionParameter>();

            // Add arguments to local scope
            foreach (var (name, type) in argNames.Zip(argTypes, (x, y) => (name: x, type: y))) {
                scope = scope.AddVariable(name, type, scope.ClosureLevel + 1);
                args.Add(new FunctionParameter(name, type));
            }

            scope = scope.IncrementClosureLevel();
            this.scopes.Push(scope);

            // Handle body
            var body = bodyFactory();
            this.scopes.Pop();

            // Rewrite function to handle closed variables
            var closedVariables = new ClosureAnalyzer(body).Analyze();

            //foreach (var closedVar in closedVariables) {
            //    body = new VariableDefinitionSyntax(
            //        closedVar.Name,
            //        new ClosureEnvironmentAccessSyntax(                        
            //            closedVar.Name,
            //            closedVar.Type,
            //            this.scopes.Peek()
            //        ),
            //        body,
            //        this.scopes.Peek()
            //    );
            //}

            var funcLiteral = new FunctionLiteralSyntax(
                body,
                this.scopes.Peek(), 
                body.ExpressionType,
                closedVariables.Select(x => x).ToArray(),
                args.ToArray()
            );

            return funcLiteral;
        }

        public ISyntaxTree AnalyzeIfExpression(ISyntaxTree condition, ISyntaxTree affirmative, ISyntaxTree negative) {
            if (affirmative.ExpressionType != negative.ExpressionType) {
                throw new Exception("Branches of an if expression must have the same return type");
            }

            if (condition.ExpressionType != PrimitiveTrophyType.Boolean) {
                throw new Exception("The condition of an if statement must be a boolean");
            }

            return new IfExpressionSyntax(condition, affirmative, negative, affirmative.ExpressionType, this.scopes.Peek());
        }

        public ISyntaxTree AnalyzeConstantDefinition(string name, ISyntaxTree assign, Func<ISyntaxTree> appendixFactory) {
            var scope = this.scopes.Peek();

            if (scope.Variables.ContainsKey(name)) {
                throw new Exception($"Variable '{name}' is already defined in the current scope");
            }

            scope = scope.AddVariable(name, assign.ExpressionType, scope.ClosureLevel, false);
            this.scopes.Push(scope);
            var appendix = appendixFactory();
            this.scopes.Pop();

            return new VariableDefinitionSyntax(name, assign, appendix, this.scopes.Peek());
        }

        public ISyntaxTree AnalyzeBinaryXor(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (
                operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type 
                || operand1.ExpressionType == PrimitiveTrophyType.Boolean && operand2.ExpressionType == PrimitiveTrophyType.Boolean
            ) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.Xor,
                    PrimitiveTrophyType.Int64Type,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'xor' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeBinaryOr(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.BitwiseOr,
                    PrimitiveTrophyType.Int64Type,
                    this.scopes.Peek()
                );
            }
            else if (operand1.ExpressionType == PrimitiveTrophyType.Boolean && operand2.ExpressionType == PrimitiveTrophyType.Boolean) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.LogicalOr,
                    PrimitiveTrophyType.Boolean,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'and' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeBinaryAnd(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.BitwiseAnd,
                    PrimitiveTrophyType.Int64Type,
                    this.scopes.Peek()
                );
            }
            else if (operand1.ExpressionType == PrimitiveTrophyType.Boolean && operand2.ExpressionType == PrimitiveTrophyType.Boolean) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.LogicalAnd,
                    PrimitiveTrophyType.Boolean,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'and' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeBinaryAddition(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.Add,
                    PrimitiveTrophyType.Int64Type,
                    this.scopes.Peek()
                );
            }
            else if (operand1.ExpressionType == PrimitiveTrophyType.Real64Type && operand2.ExpressionType == PrimitiveTrophyType.Real64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.Add,
                    PrimitiveTrophyType.Real64Type,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'add' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeBinarySubtraction(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.Subtract,
                    PrimitiveTrophyType.Int64Type,
                    this.scopes.Peek()
                );
            }
            else if (operand1.ExpressionType == PrimitiveTrophyType.Real64Type && operand2.ExpressionType == PrimitiveTrophyType.Real64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.Subtract,
                    PrimitiveTrophyType.Real64Type,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'subtract' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeBinaryRealDivision(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (
                operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type
                || operand1.ExpressionType == PrimitiveTrophyType.Real64Type && operand2.ExpressionType == PrimitiveTrophyType.Real64Type
            ) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.RealDivide,
                    PrimitiveTrophyType.Real64Type,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'divide' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeBinaryStrictDivision(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.StrictDivide,
                    PrimitiveTrophyType.Int64Type,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'strict divide' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }


        public ISyntaxTree AnalyzeBinaryMultiplication(ISyntaxTree operand1, ISyntaxTree operand2) {
            if (operand1.ExpressionType == PrimitiveTrophyType.Int64Type && operand2.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new BinaryExpressionSyntax(
                    operand1, 
                    operand2,
                    BinaryOperator.Multiply,
                    PrimitiveTrophyType.Int64Type, 
                    this.scopes.Peek()
                );
            }
            else if (operand1.ExpressionType == PrimitiveTrophyType.Real64Type && operand2.ExpressionType == PrimitiveTrophyType.Real64Type) {
                return new BinaryExpressionSyntax(
                    operand1,
                    operand2,
                    BinaryOperator.Multiply,
                    PrimitiveTrophyType.Real64Type,
                    this.scopes.Peek()
                );
            }

            throw new Exception($"The operation 'multiply' is not defined for the types '{operand1.ExpressionType}' and '{operand2.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeUnaryNegation(ISyntaxTree operand) {
            if (operand.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new UnaryExpressionSyntax(operand, UnaryOperator.Negation, PrimitiveTrophyType.Int64Type, this.scopes.Peek());
            }
            else if (operand.ExpressionType == PrimitiveTrophyType.Real64Type) {
                return new UnaryExpressionSyntax(operand, UnaryOperator.Negation, PrimitiveTrophyType.Real64Type, this.scopes.Peek());
            }

            throw new Exception($"The operation 'negate' is not defined for the type '{operand.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeUnaryNot(ISyntaxTree operand) {
            if (operand.ExpressionType == PrimitiveTrophyType.Int64Type) {
                return new UnaryExpressionSyntax(operand, UnaryOperator.Not, PrimitiveTrophyType.Int64Type, this.scopes.Peek());
            }

            throw new Exception($"The operation 'not' is not defined for the type '{operand.ExpressionType}'");
        }

        public ISyntaxTree AnalyzeVariableLiteral(string name) {
            var scope = this.scopes.Peek();

            if (!scope.Variables.TryGetValue(name, out var info)) {
                throw new Exception($"Variable '{name}' does not exist in the current scope");
            }

            return new VariableLiteralSyntax(name, info.Type, scope);
        }

        public ISyntaxTree AnalyzeInt64Literal(long value) {
            return new Int64LiteralSyntax(value, this.scopes.Peek());
        }

        public ISyntaxTree AnalyzeBoolLiteral(bool value) {
            return new BoolLiteralSyntax(value, this.scopes.Peek());
        }

        public ISyntaxTree AnalyzeReal64Literal(double value) {
            return new Real64Literal(value, this.scopes.Peek());
        }
    }
}