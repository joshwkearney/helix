using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Attempt20.Features.Primitives;

namespace Attempt20.CodeGeneration {
    public static class CHelper {
        public static void Indent(int level, StringBuilder sb) {
            sb.Append(' ', level * 4);
        }
    }

    public class CStatementWriter : ICStatementWriter {
        public event EventHandler<CStatement> StatementWritten;

        public void WriteStatement(CStatement stat) {
            this.StatementWritten?.Invoke(this, stat);
        }
    }

    public abstract class CType {
        public static CType Integer { get; } = new CPrimitiveType("int");

        public static CType VoidPointer { get; } = Pointer(NamedType("void"));

        public static CType Pointer(CType inner) {
            return new CPointerType(inner);
        }

        public static CType NamedType(string name) {
            return new CPrimitiveType(name);
        }

        private CType() { }

        private class CPrimitiveType : CType {
            private readonly string name;

            public CPrimitiveType(string name) {
                this.name = name;
            }

            public override string ToString() {
                return this.name;
            }
        }

        private class CPointerType : CType {
            private readonly CType innerType;

            public CPointerType(CType inner) {
                this.innerType = inner;
            }

            public override string ToString() {
                return this.innerType.ToString() + "*";
            }
        }
    }

    public class CParameter {
        public string Name { get; }

        public CType Type { get; }

        public CParameter(CType type, string name) {
            this.Type = type;
            this.Name = name;
        }
    }

    public abstract class CDeclaration {
        public static CDeclaration Function(CType returnType, string name, IReadOnlyList<CParameter> pars, IReadOnlyList<CStatement> stats) {
            return new CFunctionDeclaration(returnType, name, pars, Option.Some(stats));
        }

        public static CDeclaration Function(string name, IReadOnlyList<CParameter> pars, IReadOnlyList<CStatement> stats) {
            return new CFunctionDeclaration(name, pars, Option.Some(stats));
        }

        public static CDeclaration FunctionPrototype(CType returnType, string name, IReadOnlyList<CParameter> pars) {
            return new CFunctionDeclaration(returnType, name, pars, Option.None<IReadOnlyList<CStatement>>());
        }

        public static CDeclaration FunctionPrototype(string name, IReadOnlyList<CParameter> pars) {
            return new CFunctionDeclaration(name, pars, Option.None<IReadOnlyList<CStatement>>());
        }

        public static CDeclaration Struct(string name, IReadOnlyList<CParameter> members) {
            return new CStructDeclaration(name, Option.Some(members));
        }

        public static CDeclaration StructPrototype(string name) {
            return new CStructDeclaration(name, Option.None<IReadOnlyList<CParameter>>());
        }

        public static CDeclaration EmptyLine() {
            return new CEmptyLine();
        }

        private CDeclaration() { }

        public abstract void WriteToC(int indentLevel, StringBuilder sb);

        private class CEmptyLine : CDeclaration {
            public override void WriteToC(int indentLevel, StringBuilder sb) {
                sb.AppendLine();
            }
        }

        private class CStructDeclaration : CDeclaration {
            private readonly string Name;
            private readonly IOption<IReadOnlyList<CParameter>> members;

            public CStructDeclaration(string name, IOption<IReadOnlyList<CParameter>> members) {
                this.Name = name;
                this.members = members;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);

                if (this.members.TryGetValue(out var mems)) {
                    sb.Append("struct ").Append(this.Name).AppendLine(" {");

                    foreach (var mem in mems) {
                        CHelper.Indent(indentLevel + 1, sb);
                        sb.Append(mem.Type.ToString()).Append(" ").Append(mem.Name).AppendLine(";");
                    }

                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("};");
                }
                else {
                    sb.Append("typedef struct ").Append(this.Name).Append(" ").Append(this.Name).AppendLine(";");
                }
            }
        }

        private class CFunctionDeclaration : CDeclaration {
            private readonly IOption<CType> ReturnType;
            private readonly string name;
            private readonly IReadOnlyList<CParameter> pars;
            private readonly IOption<IReadOnlyList<CStatement>> stats;

            public CFunctionDeclaration(CType returnType, string name, IReadOnlyList<CParameter> pars, IOption<IReadOnlyList<CStatement>> stats) {
                this.ReturnType = Option.Some(returnType);
                this.name = name;
                this.pars = pars;
                this.stats = stats;
            }

            public CFunctionDeclaration(string name, IReadOnlyList<CParameter> pars, IOption<IReadOnlyList<CStatement>> stats) {
                this.ReturnType = Option.None<CType>();
                this.name = name;
                this.pars = pars;
                this.stats = stats;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);

                if (this.ReturnType.TryGetValue(out var type)) {
                    sb.Append(type);
                }
                else {
                    sb.Append("void");
                }

                sb.Append(" ").Append(this.name).Append("(");

                if (this.pars.Any()) {
                    sb.Append(this.pars[0].Type).Append(" ").Append(this.pars[0].Name);

                    foreach (var par in this.pars.Skip(1)) {
                        sb.Append(", ").Append(par.Type).Append(" ").Append(par.Name);
                    }
                }

                sb.Append(")");

                if (this.stats.TryGetValue(out var stats)) {
                    if (stats.Any()) {
                        sb.AppendLine(" {");
                    }

                    foreach (var stat in stats) {
                        stat.WriteToC(indentLevel + 1, sb);
                    }

                    if (stats.Any()) {
                        CHelper.Indent(indentLevel, sb);
                    }

                    sb.AppendLine("}");
                }
                else {
                    sb.AppendLine(";");
                }                
            }
        }
    }

    public abstract class CStatement {
        public static CStatement VariableDeclaration(CType type, string name, CExpression assign) {
            return new CVariableDeclaration(type, name, Option.Some(assign));
        }

        public static CStatement VariableDeclaration(CType type, string name) {
            return new CVariableDeclaration(type, name, Option.None<CExpression>());
        }

        public static CStatement Return(CExpression expr) {
            return new CReturnStatement(expr);
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

        public static CStatement Break() {
            return new CBreakStatement();
        }

        public static CStatement NewLine() {
            return new CNewLine();
        }

        private CStatement() { }

        public abstract void WriteToC(int indentLevel, StringBuilder sb);

        private class CBreakStatement : CStatement {
            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("break;");
            }
        }

        private class WhileStatement : CStatement {
            public CExpression Conditon { get; set; }

            public IReadOnlyList<CStatement> Body { get; set; }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("while (").Append(this.Conditon).Append(") {");

                if (this.Body.Any()) {
                    sb.AppendLine();
                }

                foreach (var stat in this.Body) {
                    stat.WriteToC(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }

        private class ExpressionStatement : CStatement {
            public CExpression Expression { get; set; }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.Expression).AppendLine(";");
            }
        }

        private class CAssignment : CStatement {
            private readonly CExpression left;
            private readonly CExpression right;

            public CAssignment(CExpression left, CExpression right) {
                this.left = left;
                this.right = right;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.left).Append(" = ").Append(this.right).AppendLine(";");
            }
        }

        private class CArrayDeclaration : CStatement {
            public CExpression ArraySize { get; set; }

            public CType ArrayType { get; set; }

            public string Name { get; set; }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.ArrayType).Append(" ").Append(this.Name).Append("[").Append(this.ArraySize).AppendLine("];");
            }
        }

        private class CVariableDeclaration : CStatement {
            private readonly CType varType;
            private readonly string name;
            private readonly IOption<CExpression> assignExpr;

            public CVariableDeclaration(CType type, string name, IOption<CExpression> assign) {
                this.varType = type;
                this.name = name;
                this.assignExpr = assign;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.varType.ToString()).Append(" ").Append(this.name);

                if (this.assignExpr.TryGetValue(out var assign)) {
                    sb.Append(" = ").Append(assign.ToString());
                }

                sb.AppendLine(";");
            }
        }

        private class CReturnStatement : CStatement {
            private readonly CExpression value;

            public CReturnStatement(CExpression value) {
                this.value = value;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("return ").Append(this.value.ToString()).AppendLine(";");
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

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("if (").Append(this.cond.ToString()).Append(") { ");

                if (this.affirmBranch.Any()) {
                    sb.AppendLine();

                    foreach (var stat in this.affirmBranch) {
                        stat.WriteToC(indentLevel + 1, sb);
                    }
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("} ");

                if (this.negBranch.Any()) {
                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("else {");

                    foreach (var stat in this.negBranch) {
                        stat.WriteToC(indentLevel + 1, sb);
                    }

                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("}");
                }
            }
        }

        private class CNewLine : CStatement {
            public override void WriteToC(int indentLevel, StringBuilder sb) {
                sb.AppendLine();
            }
        }
    }

    public abstract class CExpression {
        public static CExpression IntLiteral(int value) {
            return new CIntExpression(value, CIntKind.Standard);
        }

        public static CExpression BinaryExpression(CExpression left, CExpression right, BinaryOperation op) {
            return new CBinaryExpression(left, right, op);
        }

        public static CExpression VariableLiteral(string name) {
            return new CVariableLiteral(name);
        }

        public static CExpression Dereference(CExpression target) {
            if (target is CUnaryExpression expr && expr.op == "&") {
                return expr.target;
            }

            return new CUnaryExpression("*", target);
        }

        public static CExpression Not(CExpression target) {
            if (target is CUnaryExpression expr && expr.op == "&") {
                return expr.target;
            }

            return new CUnaryExpression("!", target);
        }

        public static CExpression AddressOf(CExpression target) {
            if (target is CUnaryExpression expr && expr.op == "*") {
                return expr.target;
            }

            return new CUnaryExpression("&", target);
        }

        public static CExpression Invoke(CExpression target, IReadOnlyList<CExpression> args) {
            return new CFunctionInvoke() {
                Target = target,
                Arguments = args
            };
        }

        public static CExpression Sizeof(CType type) {
            return Invoke(VariableLiteral("sizeof"), new[] { VariableLiteral(type.ToString()) });
        }

        public static CExpression Sizeof(CExpression expr) {
            return Invoke(VariableLiteral("sizeof"), new[] { expr });
        }

        public static CExpression StringLiteral(string value) {
            return new CStringLiteral() { Value = value };
        }

        public static CExpression MemberAccess(CExpression target, string memberName) {
            return new CMemberAccess() {
                Target = target,
                MemberName = memberName
            };
        }

        public static CExpression ArrayIndex(CExpression target, CExpression index) {
            return new CArrayIndex() {
                Target = target,
                Index = index
            };
        }

        private CExpression() { }

        private enum CIntKind {
            Standard, Long, LongLong
        }

        private class CStringLiteral : CExpression {
            public string Value { get; set; }

            public override string ToString() {
                return "\"" + this.Value + "\"";
            }
        }

        private class CArrayIndex : CExpression {
            public CExpression Target { get; set; }

            public CExpression Index { get; set; }

            public override string ToString() {
                return this.Target + "[" + this.Index + "]";
            }
        }

        private class CMemberAccess : CExpression {
            public CExpression Target { get; set; }

            public string MemberName { get; set; }

            public override string ToString() {
                return "(" + this.Target + "." + this.MemberName + ")";
            }
        }

        private class CFunctionInvoke : CExpression {
            public CExpression Target { get; set; }

            public IReadOnlyList<CExpression> Arguments { get; set; }

            public override string ToString() {
                return this.Target.ToString() + "(" + string.Join(", ", this.Arguments) + ")";
            }
        }

        private class CIntExpression : CExpression {
            private readonly int value;
            private readonly CIntKind kind;

            public CIntExpression(int value, CIntKind kind) {
                this.value = value;
                this.kind = kind;
            }

            public override string ToString() {
                switch (this.kind) {
                    case CIntKind.Long:
                        return this.value.ToString() + "L";
                    case CIntKind.LongLong:
                        return this.value.ToString() + "LL";
                    default:
                        return this.value.ToString();
                }
            }
        }

        private class CBinaryExpression : CExpression {
            private readonly CExpression left, right;
            private readonly BinaryOperation op;

            public CBinaryExpression(CExpression left, CExpression right, BinaryOperation op) {
                this.left = left;
                this.right = right;
                this.op = op;
            }

            public override string ToString() {
                var op = this.op switch {
                    BinaryOperation.Add => "+",
                    BinaryOperation.And => "&&",
                    BinaryOperation.EqualTo => "==",
                    BinaryOperation.GreaterThan => ">",
                    BinaryOperation.GreaterThanOrEqualTo => ">=",
                    BinaryOperation.LessThan => "<",
                    BinaryOperation.LessThanOrEqualTo => "<=",
                    BinaryOperation.Multiply => "*",
                    BinaryOperation.NotEqualTo => "!=",
                    BinaryOperation.Or => "||",
                    BinaryOperation.Subtract => "-",
                    BinaryOperation.Xor => "^",
                    _ => throw new Exception()
                };

                return "(" + this.left.ToString() + " " + op + " " + this.right.ToString() + ")";
            }
        }

        private class CUnaryExpression : CExpression {
            public readonly string op;
            public readonly CExpression target;

            public CUnaryExpression(string op, CExpression target) {
                this.op = op;
                this.target = target;
            }

            public override string ToString() {
                return "(" + this.op + this.target + ")";
            }
        }

        private class CVariableLiteral : CExpression {
            private readonly string name;

            public CVariableLiteral(string name) {
                this.name = name;
            }

            public override string ToString() {
                return this.name;
            }
        }
    }
}
