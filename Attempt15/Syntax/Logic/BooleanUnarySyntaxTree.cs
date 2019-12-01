using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax.Logic {
    public class BooleanUnarySyntaxTree : ISyntaxTree {
        public ISyntaxTree Operand { get; }

        public BooleanUnaryOperationKind Operation { get; }

        public ITrophyType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        public BooleanUnarySyntaxTree(ISyntaxTree operand, BooleanUnaryOperationKind kind, ITrophyType returnType) {
            this.Operand = operand;
            this.Operation = kind;
            this.ExpressionType = returnType;
            this.ExternalVariables = operand.ExternalVariables;
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            var operand = this.Operand.GenerateCode(args);

            switch (this.Operation) {
                case BooleanUnaryOperationKind.ConvertBoolToInt:
                    var intName = SimpleType.Int.GenerateName(args);
                    return $"(({intName}){operand})";
                case BooleanUnaryOperationKind.ConvertBoolToReal:
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
