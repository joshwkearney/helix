using System;
using System.Collections.Generic;
using Trophy.Features.Primitives;

namespace Trophy.CodeGeneration.CSyntax {
    public abstract class CExpression {
        public static CExpression IntLiteral(int value) {
            return new CIntExpression(value);
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

        public static CExpression Cast(CType type, CExpression arg) {
            return new CCastExpression(type, arg);
        }

        private CExpression() { }

        private class CCastExpression : CExpression {
            private readonly CExpression argument;
            private readonly CType type;

            public CCastExpression(CType type, CExpression argument) {
                this.type = type;
                this.argument = argument;
            }

            public override string ToString() {
                return "(" + this.type + ")" + argument;
            }
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

            public CIntExpression(int value) {
                this.value = value;
            }

            public override string ToString() {
                return value + "U";
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
                    BinaryOperation.And => "&",
                    BinaryOperation.EqualTo => "==",
                    BinaryOperation.GreaterThan => ">",
                    BinaryOperation.GreaterThanOrEqualTo => ">=",
                    BinaryOperation.LessThan => "<",
                    BinaryOperation.LessThanOrEqualTo => "<=",
                    BinaryOperation.Multiply => "*",
                    BinaryOperation.NotEqualTo => "!=",
                    BinaryOperation.Or => "|",
                    BinaryOperation.Subtract => "-",
                    BinaryOperation.Xor => "^",
                    BinaryOperation.Modulo => "%",
                    BinaryOperation.FloorDivide => "/",
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
