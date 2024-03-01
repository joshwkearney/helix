namespace Helix.Common.Hmm {
    public class HmmStringifier : IHmmVisitor<string> {
        private int indent = 0;

        public string VisitAddressOf(HmmAddressOf syntax) {
            return this.GetIndent() + $"let {syntax.Result} = &{syntax.Operand};\n";
        }

        public string VisitArrayLiteral(HmmArrayLiteral syntax) {
            return this.GetIndent() + $"let {syntax.Result} = [{string.Join(", ", syntax.Args)}];\n";
        }

        public string VisitAssignment(HmmAssignment syntax) {
            return this.GetIndent() + syntax.Variable + " = " + syntax.Value + ";\n";
        }

        public string VisitAsSyntax(HmmAsSyntax syntax) {
            return this.GetIndent() + $"let {syntax.Result} = {syntax.Operand} as {syntax.Type};\n";
        }

        public string VisitBinarySyntax(HmmBinarySyntax syntax) {
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

            return this.GetIndent() + $"let {syntax.Result} = {syntax.Left} {op} {syntax.Right};\n";
        }

        public string VisitBreak(HmmBreakSyntax syntax) {
            return this.GetIndent() + "break;\n";
        }

        public string VisitContinue(HmmContinueSyntax syntax) {
            return this.GetIndent() + "continue;\n";
        }

        public string VisitDereference(HmmDereference syntax) {
            var var = syntax.IsLValue ? "ref" : "let";

            return this.GetIndent() + $"{var} {syntax.Result} = *{syntax.Operand};\n";
        }

        public string VisitFunctionDeclaration(HmmFunctionDeclaration syntax) {
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

        public string VisitIfExpression(HmmIfExpression syntax) {
            if (syntax.Affirmative == "void" && syntax.Negative == "void") {
                if (syntax.NegativeBody.Any()) {
                    var result = this.GetIndent() + "if " + syntax.Condition + " then {\n";

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
                    var result = this.GetIndent() + "if " + syntax.Condition + " then {\n";

                    this.indent++;
                    foreach (var line in syntax.AffirmativeBody) {
                        result += line.Accept(this);
                    }
                    this.indent--;

                    result += this.GetIndent() + "}\n";
                    return result;
                }
            }
            else {
                var result = this.GetIndent() + "let " + syntax.Result + ";\n";
                result += this.GetIndent() + "if " + syntax.Condition + " then {\n";

                this.indent++;
                foreach (var line in syntax.AffirmativeBody) {
                    result += line.Accept(this);
                }
                result += this.GetIndent() + syntax.Result + " = " + syntax.Affirmative + ";\n";
                this.indent--;

                result += this.GetIndent() + "}\n";
                result += this.GetIndent() + "else {\n";

                this.indent++;
                foreach (var line in syntax.NegativeBody) {
                    result += line.Accept(this);
                }
                result += this.GetIndent() + syntax.Result + " = " + syntax.Negative + ";\n";
                this.indent--;

                result += this.GetIndent() + "}\n";
                return result;
            }
        }

        public string VisitIndex(HmmIndex syntax) {
            var var = syntax.IsLValue ? "ref" : "let";

            return this.GetIndent() + $"{var} {syntax.Result} = {syntax.Operand}[{syntax.Index}];\n";
        }

        public string VisitInvoke(HmmInvokeSyntax syntax) {
            return this.GetIndent() + $"let {syntax.Result} = {syntax.Target}({string.Join(", ", syntax.Arguments)});\n";
        }

        public string VisitIs(HmmIsSyntax syntax) {
            return this.GetIndent() + $"let {syntax.Result} = {syntax.Operand} is {syntax.Field};\n";
        }

        public string VisitLoop(HmmLoopSyntax syntax) {
            var result = this.GetIndent() + "loop {\n";

            this.indent++;
            foreach (var line in syntax.Body) {
                result += line.Accept(this);
            }
            this.indent--;

            result += this.GetIndent() + "}\n";

            return result;
        }

        public string VisitMemberAccess(HmmMemberAccess syntax) {
            var var = syntax.IsLValue ? "ref" : "let";

            return this.GetIndent() + $"{var} {syntax.Result} = {syntax.Operand}.{syntax.Member};\n";
        }

        public string VisitNew(HmmNewSyntax syntax) {
            var assignments = syntax.Assignments.Select(x => {
                if (x.Field.TryGetValue(out var field)) {
                    return field + " = " + x.Value;
                }
                else {
                    return x.Value;
                }
            });

            var result = this.GetIndent() + $"let {syntax.Result} = new {syntax.Type}";

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

        public string VisitUnaryOperator(HmmUnaryOperator syntax) {
            if (syntax.Operator == UnaryOperatorKind.AddressOf) {
                return this.GetIndent() + $"let {syntax.Result} = &{syntax.Operand};\n";
            }
            else if (syntax.Operator == UnaryOperatorKind.Dereference) {
                return this.GetIndent() + $"let {syntax.Result} = *{syntax.Operand};\n";
            }
            else {
                var op = syntax.Operator switch {
                    UnaryOperatorKind.Minus => "-",
                    UnaryOperatorKind.Plus => "+",
                    UnaryOperatorKind.Not => "!",
                    _ => throw new NotImplementedException(),
                };

                return this.GetIndent() + $"let {syntax.Result} = {op}{syntax.Operand};\n";
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

        public string VisitVariableStatement(HmmVariableStatement syntax) {
            if (syntax.IsMutable) {
                return this.GetIndent() + $"var {syntax.Variable} = {syntax.Value};\n";
            }
            else {
                return this.GetIndent() + $"let {syntax.Variable} = {syntax.Value};\n";
            }
        }

        private string GetIndent() => new string(' ', 4 * this.indent);
    }
}
