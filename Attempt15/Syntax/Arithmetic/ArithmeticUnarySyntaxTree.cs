using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Arithmetic {
    public class ArithmeticUnarySyntaxTree : ISyntaxTree {
        public ISyntaxTree Operand { get; }

        public ArithmeticUnaryOperationKind Operation { get; }

        public ITrophyType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        public ArithmeticUnarySyntaxTree(ISyntaxTree operand, ArithmeticUnaryOperationKind kind, ITrophyType returnType) {
            this.Operand = operand;
            this.Operation = kind;
            this.ExpressionType = returnType;
            this.ExternalVariables = operand.ExternalVariables;
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            switch (this.Operation) {
                case ArithmeticUnaryOperationKind.ConvertIntToReal:
                    var operand = this.Operand.GenerateCode(args);

                    return $"(1.0 * {operand})";
                default:
                    throw new Exception();
            }
        }

        public bool DoesVariableEscape(string variableName) {
            return this.Operand.DoesVariableEscape(variableName);
        }
    }
}
