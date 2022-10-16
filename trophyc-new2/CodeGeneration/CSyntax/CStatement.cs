using System.Text;

namespace Trophy.CodeGeneration.CSyntax {
    public abstract class CStatement {
        public static CStatement VariableDeclaration(CType type, string name, CExpression assign) {
            return new CVariableDeclaration(type, name, Option.Some(assign));
        }

        public static CStatement VariableDeclaration(CType type, string name) {
            return new CVariableDeclaration(type, name, Option.None);
        }

        public static CStatement Return(CExpression expr) {
            return new CReturnStatement(Option.Some(expr));
        }

        public static CStatement Return() {
            return new CReturnStatement(Option.None);
        }

        public static CStatement If(CExpression cond, IReadOnlyList<CStatement> affirm, IReadOnlyList<CStatement> neg) {
            return new CIfStatement(cond, affirm, neg);
        }

        public static CStatement If(CExpression cond, IReadOnlyList<CStatement> affirm) {
            return If(cond, affirm, new CStatement[] { });
        }

        public static CStatement Assignment(CExpression left, CExpression right) {
            return new CAssignment(left, right);
        }

        public static CStatement FromExpression(CExpression expr) {
            return new ExpressionStatement() {
                Expression = expr
            };
        }

        public static CStatement ArrayDeclaration(CType elementType, string name, CExpression size) {
            Func<int, int> x = (y => y + 1);

            return new CArrayDeclaration() {
                ArraySize = size,
                ArrayType = elementType,
                Name = name
            };
        }

        public static CStatement While(CExpression cond, IReadOnlyList<CStatement> body) {
            return new WhileStatement() {
                Conditon = cond,
                Body = body
            };
        }

        public static CStatement For(CType iteratorType, string iteratorName, CExpression start, 
                                     CExpression end, IReadOnlyList<CStatement> body) {
            return new ForStatement() {
                IteratorName = iteratorName,
                IteratorType = iteratorType,
                Start = start,
                End = end,
                Body = body
            };
        }


        public static CStatement Break() {
            return new CBreakStatement();
        }

        public static CStatement CaseLabel(CExpression value, IReadOnlyList<CStatement> stats) {
            return new CCaseStatement(value, stats);
        }

        public static CStatement DefaultLabel(IReadOnlyList<CStatement> stats) {
            return new CDefaultLabel(stats);
        }

        public static CStatement SwitchStatement(CExpression value, IReadOnlyList<CStatement> cases) {
            return new CSwitchStatement(value, cases);
        }

        public static CStatement Comment(string value) {
            return new CCommentStatement(value);
        }

        public static CStatement NewLine() {
            return new CNewLine();
        }

        public virtual bool IsEmpty => false;

        private CStatement() { }

        public abstract void Write(int indentLevel, StringBuilder sb);

        private class CSwitchStatement : CStatement {
            private readonly CExpression value;
            private readonly IReadOnlyList<CStatement> cases;

            public CSwitchStatement(CExpression value, IReadOnlyList<CStatement> cases) {
                this.value = value;
                this.cases = cases;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("switch (").Append(this.value).AppendLine(") {");

                foreach (var stat in this.cases) {
                    stat.Write(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}").AppendLine();
            }
        }

        private class CDefaultLabel : CStatement {
            private readonly IReadOnlyList<CStatement> body;

            public CDefaultLabel(IReadOnlyList<CStatement> body) {
                this.body = body;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("default: {");

                foreach (var stat in this.body) {
                    stat.Write(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }

        private class CCaseStatement : CStatement {
            private readonly CExpression value;
            private readonly IReadOnlyList<CStatement> body;

            public CCaseStatement(CExpression value, IReadOnlyList<CStatement> body) {
                this.value = value;
                this.body = body;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("case ").Append(this.value).AppendLine(": {");

                foreach (var stat in this.body) {
                    stat.Write(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }

        private class CCommentStatement : CStatement {
            private readonly string value;

            public CCommentStatement(string value) {
                this.value = value;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("// ").AppendLine(this.value);
            }
        }

        private class CBreakStatement : CStatement {
            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("break;");
            }
        }

        private class WhileStatement : CStatement {
            public CExpression Conditon { get; set; }

            public IReadOnlyList<CStatement> Body { get; set; }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("while (").Append(this.Conditon).Append(") {");

                if (this.Body.Any()) {
                    sb.AppendLine();
                }

                foreach (var stat in this.Body) {
                    stat.Write(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }

        private class ForStatement : CStatement {
            public string IteratorName { get; set; }

            public CType IteratorType { get; set; }

            public CExpression Start { get; set; }

            public CExpression End { get; set; }

            public IReadOnlyList<CStatement> Body { get; set; }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("for (");
                sb.Append(this.IteratorType).Append(' ').Append(this.IteratorName);
                sb.Append(" = ").Append(this.Start).Append("; ");
                sb.Append(this.IteratorName).Append(" < ").Append(this.End).Append("; ");
                sb.Append(this.IteratorName).Append(" += 1) {").AppendLine();

                foreach (var stat in this.Body) {
                    stat.Write(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }

        private class ExpressionStatement : CStatement {
            public CExpression Expression { get; set; }

            public override void Write(int indentLevel, StringBuilder sb) {
                var expr = this.Expression.ToString();

                if (expr != "0U") {
                    CHelper.Indent(indentLevel, sb);
                    sb.Append(this.Expression).AppendLine(";");
                }
            }
        }

        private class CAssignment : CStatement {
            private readonly CExpression left;
            private readonly CExpression right;

            public CAssignment(CExpression left, CExpression right) {
                this.left = left;
                this.right = right;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.left).Append(" = ").Append(this.right).AppendLine(";");
            }
        }

        private class CArrayDeclaration : CStatement {
            public CExpression ArraySize { get; set; }

            public CType ArrayType { get; set; }

            public string Name { get; set; }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.ArrayType).Append(" ").Append(this.Name).Append("[").Append(this.ArraySize).AppendLine("];");
            }
        }

        private class CVariableDeclaration : CStatement {
            private readonly CType varType;
            private readonly string name;
            private readonly Option<CExpression> assignExpr;

            public CVariableDeclaration(CType type, string name, Option<CExpression> assign) {
                this.varType = type;
                this.name = name;
                this.assignExpr = assign;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.varType.ToString()).Append(" ").Append(this.name);

                if (this.assignExpr.TryGetValue(out var assign)) {
                    sb.Append(" = ").Append(assign.ToString());
                }

                sb.AppendLine(";");
            }
        }

        private class CReturnStatement : CStatement {
            private readonly Option<CExpression> value;

            public CReturnStatement(Option<CExpression> value) {
                this.value = value;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);

                if (this.value.TryGetValue(out var value)) {
                    sb.Append("return ").Append(value).AppendLine(";");
                }
                else {
                    sb.AppendLine("return;");
                }
            }
        }

        private class CIfStatement : CStatement {
            private readonly CExpression cond;
            private readonly IReadOnlyList<CStatement> affirmBranch;
            private readonly IReadOnlyList<CStatement> negBranch;

            public CIfStatement(CExpression cond, IReadOnlyList<CStatement> affirm, IReadOnlyList<CStatement> neg) {
                this.cond = cond;
                this.affirmBranch = affirm;
                this.negBranch = neg;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("if (").Append(this.cond.ToString()).Append(") { ");

                if (this.affirmBranch.Any()) {
                    sb.AppendLine();

                    foreach (var stat in this.affirmBranch) {
                        stat.Write(indentLevel + 1, sb);
                    }
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("} ");

                if (this.negBranch.Any()) {
                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("else {");

                    foreach (var stat in this.negBranch) {
                        stat.Write(indentLevel + 1, sb);
                    }

                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("}");
                }
            }
        }

        private class CNewLine : CStatement {
            public override bool IsEmpty => true;

            public override void Write(int indentLevel, StringBuilder sb) {
                sb.AppendLine();
            }
        }
    }
}
