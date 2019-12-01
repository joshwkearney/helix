using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Arithmetic {
    public class ArithmeticBinarySyntaxTree : ISyntaxTree {
        public ISyntaxTree Left { get; }

        public ISyntaxTree Right { get; }

        public ArithmeticBinaryOperationKind Operation { get; }

        public ITrophyType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        public ArithmeticBinarySyntaxTree(ISyntaxTree left, ISyntaxTree right, ArithmeticBinaryOperationKind operation, ITrophyType returnType) {
            this.Right = right;
            this.Left = left;
            this.Operation = operation;
            this.ExpressionType = returnType;
            this.ExternalVariables = right.ExternalVariables.Union(left.ExternalVariables);
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            var right = this.Right.GenerateCode(args);
            var left = this.Left.GenerateCode(args);
            string value;

            switch (this.Operation) {
                case ArithmeticBinaryOperationKind.Addition:
                    value = $"({left} + {right})";
                    break;
                case ArithmeticBinaryOperationKind.Subtraction:
                    value = $"({left} - {right})";
                    break;
                case ArithmeticBinaryOperationKind.Multiplication:
                    value = $"({left} * {right})";
                    break;
                case ArithmeticBinaryOperationKind.Division:
                case ArithmeticBinaryOperationKind.StrictDivision:
                    value = $"({left} / {right})";
                    break;
                case ArithmeticBinaryOperationKind.GreaterThan:
                    value = $"({left} > {right})";
                    break;
                case ArithmeticBinaryOperationKind.LessThan:
                    value = $"({left} < {right})";
                    break;
                case ArithmeticBinaryOperationKind.EqualTo:
                    value = $"({left} == {right})";
                    break;
                case ArithmeticBinaryOperationKind.Spaceship:
                    value = $"({left} - {right})";
                    break;
                case ArithmeticBinaryOperationKind.Exponentiation:
                    if (this.ExpressionType.Kind == TrophyTypeKind.Float) {
                        value = $"pow({left}, {right})";
                    }
                    else if (this.ExpressionType.Kind == TrophyTypeKind.Int) {
                        value = $"int_pow({left}, {right})";
                    }
                    else {
                        throw new Exception();
                    }

                    break;
                default:
                    throw new Exception();
            }

            return value;
        }

        public bool DoesVariableEscape(string variableName) {
            return this.Left.DoesVariableEscape(variableName) || this.Right.DoesVariableEscape(variableName);
        }
    }
}