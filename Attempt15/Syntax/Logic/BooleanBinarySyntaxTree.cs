using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Logic {
    public class BooleanBinarySyntaxTree : ISyntaxTree {
        public ISyntaxTree Right { get; }

        public ISyntaxTree Left { get; }

        public BooleanBinaryOperationKind Operation { get; }

        public ITrophyType ExpressionType => new SimpleType(TrophyTypeKind.Boolean);

        public ExternalVariablesCollection ExternalVariables { get; }

        public BooleanBinarySyntaxTree(ISyntaxTree left, ISyntaxTree right, BooleanBinaryOperationKind operation) {
            this.Right = right;
            this.Left = left;
            this.Operation = operation;
            this.ExternalVariables = right.ExternalVariables.Union(left.ExternalVariables);
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            var right = this.Right.GenerateCode(args);
            var left = this.Left.GenerateCode(args);

            switch (this.Operation) {
                case BooleanBinaryOperationKind.And:
                    return $"({left} && {right})";
                case BooleanBinaryOperationKind.Or:
                    return $"({left} || {right})";
                case BooleanBinaryOperationKind.Xor:
                    return $"{left} ^ {right})";
                default:
                    throw new Exception();
            }
        }

        public bool DoesVariableEscape(string variableName) {
            return this.Right.DoesVariableEscape(variableName) || this.Left.DoesVariableEscape(variableName);
        }
    }
}