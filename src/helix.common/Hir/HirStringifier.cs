using Helix.Common.Hmm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Common.Hir {
    public class HirStringifier : IHirVisitor<string> {
        private int indent = 0;

        private string GetIndent() => new string(' ', 4 * this.indent);

        public string VisitAddressOf(HirAddressOf syntax) {
            return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = &{syntax.Operand};\n";
        }

        public string VisitArrayLiteral(HirArrayLiteral syntax) {
            return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = [{string.Join(", ", syntax.Args)}];\n";
        }

        public string VisitAssignment(HmmAssignment syntax) {
            return this.GetIndent() + syntax.Variable + " = " + syntax.Value + ";\n";
        }

        public string VisitBinarySyntax(HirBinarySyntax syntax) {
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

            return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = {syntax.Left} {op} {syntax.Right};\n";
        }

        public string VisitBreak(HmmBreakSyntax syntax) {
            return this.GetIndent() + "break;\n";
        }

        public string VisitContinue(HmmContinueSyntax syntax) {
            return this.GetIndent() + "continue;\n";
        }

        public string VisitDereference(HirDereference syntax) {
            var var = syntax.IsLValue ? "ref" : "let";

            return this.GetIndent() + $"{var} {syntax.Result} as {syntax.ResultType} = *{syntax.Operand};\n";
        }

        public string VisitFunctionDeclaration(HirFunctionDeclaration syntax) {
            string line = this.GetIndent() + $"func {syntax.Name}({string.Join(", ", syntax.Signature.Parameters)}) as {syntax.Signature.ReturnType} {{\n";

            this.indent++;

            foreach (var stat in syntax.Body) {
                line += stat.Accept(this);
            }

            this.indent--;
            line += this.GetIndent() + "}\n";

            return line;
        }

        public string VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) {
            return this.GetIndent() + $"func {syntax.Name}({string.Join(", ", syntax.Signature.Parameters)}) as {syntax.Signature.ReturnType};\n";
        }

        public string VisitIfExpression(HirIfExpression syntax) {
            if (syntax.NegativeBody.Any()) {
                var result = this.GetIndent() + "if " + syntax.Condition + " {\n";

                this.indent++;
                foreach (var line in syntax.AffirmativeBody) {
                    result += line.Accept(this);
                }
                this.indent--;

                result += this.GetIndent() + "}\n";
                result += this.GetIndent() + "else {\n";

                this.indent++;
                foreach (var line in syntax.NegativeBody) {
                    result += line.Accept(this);
                }
                this.indent--;

                result += this.GetIndent() + "}\n";
                return result;
            }
            else {
                var result = this.GetIndent() + "if " + syntax.Condition + " {\n";

                this.indent++;
                foreach (var line in syntax.AffirmativeBody) {
                    result += line.Accept(this);
                }
                this.indent--;

                result += this.GetIndent() + "}\n";
                return result;
            }
        }

        public string VisitIndex(HirIndex syntax) {
            var var = syntax.IsLValue ? "ref" : "let";

            return this.GetIndent() + $"{var} {syntax.Result} as {syntax.ResultType} = {syntax.Operand}[{syntax.Index}];\n";
        }

        public string VisitInvoke(HirInvokeSyntax syntax) {
            return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = {syntax.Target}({string.Join(", ", syntax.Arguments)});\n";
        }

        public string VisitIs(HirIsSyntax syntax) {
            return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = {syntax.Operand} is {syntax.Field};\n";
        }

        public string VisitLoop(HirLoopSyntax syntax) {
            var result = this.GetIndent() + "loop {\n";

            this.indent++;
            foreach (var line in syntax.Body) {
                result += line.Accept(this);
            }
            this.indent--;

            result += this.GetIndent() + "}\n";

            return result;
        }

        public string VisitMemberAccess(HirMemberAccess syntax) {
            var var = syntax.IsLValue ? "ref" : "let";

            return this.GetIndent() + $"{var} {syntax.Result} as {syntax.ResultType} = {syntax.Operand}.{syntax.Member};\n";
        }

        public string VisitNew(HirNewSyntax syntax) {
            var assignments = syntax.Assignments.Select(x => {
                if (x.Field.TryGetValue(out var field)) {
                    return field + " = " + x.Value;
                }
                else {
                    return x.Value;
                }
            });

            var result = this.GetIndent() + $"let {syntax.Result} = new {syntax.ResultType}";

            if (syntax.Assignments.Count > 0) {
                result += " {\n";

                this.indent++;
                foreach (var thing in assignments) {
                    result += this.GetIndent() + thing + ",\n";
                }
                this.indent--;

                result = result.Trim(',', '\n');
                result += "\n";
                result += this.GetIndent() + "};\n";
            }
            else {
                result += ";\n";
            }

            return result;
        }

        public string VisitReturn(HmmReturnSyntax syntax) {
            return this.GetIndent() + "return " + syntax.Operand + ";\n";
        }

        public string VisitStructDeclaration(HmmStructDeclaration syntax) {
            var result = this.GetIndent() + $"struct {syntax.Name} {{\n";

            this.indent++;
            foreach (var mem in syntax.Signature.Members) {
                result += this.GetIndent() + mem.Name + " as " + mem.Type + ";\n";
            }
            this.indent--;

            result += this.GetIndent() + "}\n";
            return result;
        }

        public string VisitTypeDeclaration(HmmTypeDeclaration syntax) {
            if (syntax.Kind == TypeDeclarationKind.Function) {
                return this.GetIndent() + "func " + syntax.Name + ";\n";
            }
            else if (syntax.Kind == TypeDeclarationKind.Struct) {
                return this.GetIndent() + "struct " + syntax.Name + ";\n";
            }
            else {
                return this.GetIndent() + "union " + syntax.Name + ";\n";
            }
        }

        public string VisitUnaryOperator(HirUnaryOperator syntax) {
            if (syntax.Operator == UnaryOperatorKind.AddressOf) {
                return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = &{syntax.Operand};\n";
            }
            else if (syntax.Operator == UnaryOperatorKind.Dereference) {
                return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = *{syntax.Operand};\n";
            }
            else {
                var op = syntax.Operator switch {
                    UnaryOperatorKind.Minus => "-",
                    UnaryOperatorKind.Plus => "+",
                    UnaryOperatorKind.Not => "!",
                    _ => throw new NotImplementedException(),
                };

                return this.GetIndent() + $"let {syntax.Result} as {syntax.Result} = {op}{syntax.Operand};\n";
            }
        }

        public string VisitUnionDeclaration(HmmUnionDeclaration syntax) {
            var result = this.GetIndent() + $"union {syntax.Name} {{\n";

            this.indent++;
            foreach (var mem in syntax.Signature.Members) {
                result += this.GetIndent() + mem.Name + " as " + mem.Type + ";\n";
            }
            this.indent--;

            result += this.GetIndent() + "}\n";
            return result;
        }

        public string VisitVariableStatement(HirVariableStatement syntax) {
            return this.GetIndent() + $"var {syntax.Variable} as {syntax.VariableType};\n";
        }

        public string VisitIntrinsicUnionMemberAccess(HirIntrinsicUnionMemberAccess syntax) {
            return this.GetIndent() + $"let {syntax.Result} as {syntax.ResultType} = __intrinsic_union_access({syntax.Operand}, {syntax.UnionMember});\n";
        }
    }
}
