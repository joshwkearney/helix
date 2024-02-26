using Helix.HelixMinusMinus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.common.Hmm {
    internal class HmmSyntaxToStringVisitor : IHmmVisitor {
        public string Result1 { get; private set; } = string.Empty;

        public void VisitArrayLiteral(HmmArrayLiteral syntax) {
            this.Result1 = $"let {syntax.Result} = [{string.Join(", ", syntax.Args)}];";
        }

        public void VisitAssignment(HmmAssignment syntax) {
            this.Result1 = syntax.Variable + " = " + syntax.Value + ";";
        }

        public void VisitAsSyntax(HmmAsSyntax syntax) {
            this.Result1 = $"let {syntax.Result} = {syntax.Operand} as {syntax.Type};";
        }

        public void VisitBinaryOperator(HmmBinaryOperator syntax) {
            var op = syntax.Operator switch {
                BinaryOperationKind.Add => "+",
                BinaryOperationKind.Subtract => "-",
                BinaryOperationKind.Multiply => "*",
                BinaryOperationKind.Modulo => "%",
                BinaryOperationKind.FloorDivide => "\\",
                BinaryOperationKind.And => "and",
                BinaryOperationKind.Or => "or",
                BinaryOperationKind.Xor => "xor",
                BinaryOperationKind.BranchingAnd => "and then",
                BinaryOperationKind.BranchingOr => "or else",
                BinaryOperationKind.EqualTo => "==",
                BinaryOperationKind.NotEqualTo => "!=",
                BinaryOperationKind.GreaterThan => ">",
                BinaryOperationKind.LessThan => "<",
                BinaryOperationKind.GreaterThanOrEqualTo => ">=",
                BinaryOperationKind.LessThanOrEqualTo => "<=",
                _ => throw new NotImplementedException()
            };

            this.Result1 = $"let {syntax.Result} = {syntax.Left} {op} {syntax.Right};";
        }

        public void VisitBoolLiteral(HmmBoolLiteral syntax) {
            this.Result1 = "let " + syntax.Result + " = " + (syntax.Value ? "true" : "false") + ";";
        }

        public void VisitBreak(HmmBreakSyntax syntax) {
            this.Result1 = "break;";
        }

        public void VisitContinue(HmmContinueSyntax syntax) {
            this.Result1 = "continue;";
        }

        public void VisitFunctionDeclaration(HmmFunctionDeclaration syntax) {
            this.Result1 = $"func {syntax.Name}({string.Join(", ", syntax.Function.Parameters)}) {{ ... }};";
        }

        public void VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) {
            this.Result1 = $"func {syntax.Name}({string.Join(", ", syntax.Signature.Parameters)});";
        }

        public void VisitIfExpression(HmmIfExpression syntax) {
            this.Result1 = $"let {syntax.Result}; if " + syntax.Condition + " then ... else ... ;"; 
        }

        public void VisitInvoke(HmmInvokeSyntax syntax) {
            this.Result1 = $"let {syntax.Result} = {syntax.Target}({string.Join(", ", syntax.Arguments)});";
        }

        public void VisitIs(HmmIsSyntax syntax) {
            this.Result1 = $"let {syntax.Result} = {syntax.Operand} is {syntax.Field};";
        }

        public void VisitLoop(HmmLoopSyntax syntax) {
            this.Result1 = "loop ... ;";
        }

        public void VisitMemberAccess(HmmMemberAccess syntax) {
            this.Result1 = $"let {syntax.Result} = {syntax.Operand}.{syntax.FieldName};";
        }

        public void VisitNew(HmmNewSyntax syntax) {
            var assignments = syntax.Assignments.Select(x => {
                if (x.Field.TryGetValue(out var field)) {
                    return field + " = " + x.Value;
                }
                else {
                    return x.Value;
                }
            });

            this.Result1 = $"let {syntax.Result} = new {syntax.Type} {{ {string.Join(", ", assignments)} }};";
        }

        public void VisitReturn(HmmReturnSyntax syntax) {
            this.Result1 = "return " + syntax.Operand + ";";
        }

        public void VisitStructDeclaration(HmmStructDeclaration syntax) {
            this.Result1 = $"struct {syntax.Name} {{ {string.Join("; ", syntax.Signature.Members.Select(x => x.Name + " as " + x.Type))} }};";
        }

        public void VisitTypeDeclaration(HmmTypeDeclaration syntax) {
            if (syntax.Kind == TypeDeclarationKind.Function) {
                this.Result1 = "func " + syntax.Name + ";";
            }
            else if (syntax.Kind == TypeDeclarationKind.Struct) {
                this.Result1 = "struct " + syntax.Name + ";";
            }
            else {
                this.Result1 = "union " + syntax.Name + ";";
            }
        }

        public void VisitUnaryOperator(HmmUnaryOperator syntax) {
            if (syntax.Operator == UnaryOperatorKind.AddressOf) {
                this.Result1 = $"let {syntax.Result} = &{syntax.Operand};";
            }
            else if (syntax.Operator == UnaryOperatorKind.Dereference) {
                this.Result1 = $"let {syntax.Result} = *{syntax.Operand};";
            }
            else {
                var op = syntax.Operator switch {
                    UnaryOperatorKind.Minus => "-",
                    UnaryOperatorKind.Plus => "+",
                    UnaryOperatorKind.Not => "!",
                    _ => throw new NotImplementedException(),
                };

                this.Result1 = $"let {syntax.Result} = {op}{syntax.Operand};";
            }
        }

        public void VisitUnionDeclaration(HmmUnionDeclaration syntax) {
            this.Result1 = $"union {syntax.Name} {{ {string.Join("; ", syntax.Signature.Members.Select(x => x.Name + " as " + x.Type))} }};";
        }

        public void VisitVariableAccess(HmmVariableAccess syntax) {
            this.Result1 = $"let {syntax.Result} = {syntax.Value};";
        }

        public void VisitVariableStatement(HmmVariableStatement syntax) {
            if (syntax.IsMutable) {
                this.Result1 = $"var {syntax.Variable} = {syntax.Variable};";
            }
            else {
                this.Result1 = $"let {syntax.Variable} = {syntax.Variable};";
            }
        }

        public void VisitVoidLiteral(HmmVoidLiteral syntax) {
            this.Result1 = $"let {syntax.Result} = void;";
        }

        public void VisitWordLiteral(HmmWordLiteral syntax) {
            this.Result1 = "let " + syntax.Result + " = " + syntax.Value + ";";
        }
    }
}
