using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.FlowControl;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var branching = this.TryAdvance(TokenKind.ElseKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new IfSyntax(
                        loc, 
                        first,
                        new BoolLiteral(loc, true),
                        second);
                }
                else {
                    first = new BinarySyntax(loc, first, second, BinaryOperationKind.Or);
                }
            }

            return first;
        }

        private ISyntaxTree XorExpression() {
            var first = this.AndExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.AndExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, BinaryOperationKind.Xor);
            }

            return first;
        }

        private ISyntaxTree AndExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var branching = this.TryAdvance(TokenKind.ThenKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new IfSyntax(
                        loc,
                        new UnaryParseSyntax(loc, UnaryOperatorKind.Not, first),
                        new BoolLiteral(loc, false),
                        second);
                }
                else {
                    first = new BinarySyntax(loc, first, second, BinaryOperationKind.And);
                }
            }

            return first;
        }

        private ISyntaxTree ComparisonExpression() {
            var first = this.AddExpression();
            var comparators = new Dictionary<TokenKind, BinaryOperationKind>() {
                { TokenKind.Equals, BinaryOperationKind.EqualTo }, { TokenKind.NotEquals, BinaryOperationKind.NotEqualTo },
                { TokenKind.LessThan, BinaryOperationKind.LessThan }, { TokenKind.GreaterThan, BinaryOperationKind.GreaterThan },
                { TokenKind.LessThanOrEqualTo, BinaryOperationKind.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualTo, BinaryOperationKind.GreaterThanOrEqualTo }
            };

            while (true) {
                bool worked = false;

                foreach (var (tok, _) in comparators) {
                    worked |= this.Peek(tok);
                }

                if (!worked) {
                    break;
                }

                var op = comparators[this.Advance().Kind];
                var second = this.AddExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }


        private ISyntaxTree AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.Plus) && !this.Peek(TokenKind.Minus)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.Plus ? BinaryOperationKind.Add : BinaryOperationKind.Subtract;
                var second = this.MultiplyExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxTree MultiplyExpression() {
            var first = this.PrefixExpression();

            while (true) {
                if (!this.Peek(TokenKind.Star) && !this.Peek(TokenKind.Modulo) && !this.Peek(TokenKind.Divide)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = BinaryOperationKind.Modulo;

                if (tok == TokenKind.Star) {
                    op = BinaryOperationKind.Multiply;
                }
                else if (tok == TokenKind.Divide) {
                    op = BinaryOperationKind.FloorDivide;
                }

                var second = this.PrefixExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }
    }
}

namespace Helix.Features.Primitives {
    public enum BinaryOperationKind {
        Add, Subtract, Multiply, Modulo, FloorDivide,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public record BinarySyntax : ISyntaxTree {
        private static readonly Dictionary<BinaryOperationKind, HelixType> intOperations = new() {
            { BinaryOperationKind.Add,                  PrimitiveType.Int },
            { BinaryOperationKind.Subtract,             PrimitiveType.Int },
            { BinaryOperationKind.Multiply,             PrimitiveType.Int },
            { BinaryOperationKind.Modulo,               PrimitiveType.Int },
            { BinaryOperationKind.FloorDivide,          PrimitiveType.Int },
            { BinaryOperationKind.And,                  PrimitiveType.Int },
            { BinaryOperationKind.Or,                   PrimitiveType.Int },
            { BinaryOperationKind.Xor,                  PrimitiveType.Int },
            { BinaryOperationKind.EqualTo,              PrimitiveType.Bool },
            { BinaryOperationKind.NotEqualTo,           PrimitiveType.Bool },
            { BinaryOperationKind.GreaterThan,          PrimitiveType.Bool },
            { BinaryOperationKind.LessThan,             PrimitiveType.Bool },
            { BinaryOperationKind.GreaterThanOrEqualTo, PrimitiveType.Bool },
            { BinaryOperationKind.LessThanOrEqualTo,    PrimitiveType.Bool },
        };

        private static readonly Dictionary<BinaryOperationKind, HelixType> boolOperations = new() {
            { BinaryOperationKind.And,                  PrimitiveType.Bool },
            { BinaryOperationKind.Or,                   PrimitiveType.Bool },
            { BinaryOperationKind.Xor,                  PrimitiveType.Bool },
            { BinaryOperationKind.EqualTo,              PrimitiveType.Bool },
            { BinaryOperationKind.NotEqualTo,           PrimitiveType.Bool },
        };

        private readonly ISyntaxTree left, right;
        private readonly BinaryOperationKind op;
        private readonly bool isTypeChecked = false;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.left, this.right };

        public bool IsPure { get; }

        public BinarySyntax(TokenLocation loc, ISyntaxTree left, ISyntaxTree right, 
                            BinaryOperationKind op, bool isTypeChecked = false) {
            this.Location = loc;
            this.left = left;
            this.right = right;
            this.op = op;
            this.isTypeChecked = isTypeChecked;

            this.IsPure = this.left.IsPure && this.right.IsPure;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var left = this.left.CheckTypes(types).ToRValue(types);
            var right = this.right.CheckTypes(types).ToRValue(types);

            if (left.GetReturnType(types).IsBool(types) && right.GetReturnType(types).IsBool(types)) {
                return this.CheckBoolExpresion(left, right, types);
            }
            else if (left.GetReturnType(types).IsInt(types) && right.GetReturnType(types).IsInt(types)) {
                return this.CheckIntExpresion(left, right, types);
            }
            else {
                throw TypeException.UnexpectedType(this.right.Location, left.GetReturnType(types));
            }
        }

        private ISyntaxTree CheckIntExpresion(ISyntaxTree left, ISyntaxTree right, TypeFrame types) {
            if (!intOperations.TryGetValue(this.op, out var returnType)) {
                throw TypeException.UnexpectedType(this.left.Location, left.GetReturnType(types));
            }

            left = left.UnifyFrom(right, types);
            right = right.UnifyFrom(left, types);

            var result = new BinarySyntax(this.Location, left, right, this.op, true);

            result.SetReturnType(returnType, types);
            result.SetCapturedVariables(left, right, types);
            result.SetPredicate(left, right, types);

            return result;
        }

        private ISyntaxTree CheckBoolExpresion(ISyntaxTree left, ISyntaxTree right, TypeFrame types) {
            var leftType = left.GetReturnType(types);
            var rightType = right.GetReturnType(types);

            if (!boolOperations.TryGetValue(this.op, out var ret)) {
                throw TypeException.UnexpectedType(this.left.Location, leftType);
            }

            var predicate = ISyntaxPredicate.Empty;
            var returnType = PrimitiveType.Bool as HelixType;

            if (leftType is PredicateBool leftPred && rightType is PredicateBool rightPred) {
                switch (this.op) {
                    case BinaryOperationKind.And:
                        predicate = leftPred.Predicate.And(rightPred.Predicate);
                        break;
                    case BinaryOperationKind.Or:
                        predicate = leftPred.Predicate.Or(rightPred.Predicate);
                        break;
                    case BinaryOperationKind.NotEqualTo:
                    case BinaryOperationKind.Xor:
                        predicate = leftPred.Predicate.Xor(rightPred.Predicate);
                        break;
                    case BinaryOperationKind.EqualTo:
                        predicate = leftPred.Predicate.Xor(rightPred.Predicate).Negate();
                        break;
                }

                returnType = new PredicateBool(predicate);
            }

            var result = new BinarySyntax(this.Location, left, right, this.op, true);

            result.SetReturnType(returnType, types);
            result.SetCapturedVariables(left, right, types);
            result.SetPredicate(left, right, types);

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            this.left.AnalyzeFlow(flow);
            this.right.AnalyzeFlow(flow);

            this.SetLifetimes(new LifetimeBounds(), flow);
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.isTypeChecked) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CBinaryExpression() {
                Left = this.left.GenerateCode(types, writer),
                Right = this.right.GenerateCode(types, writer),
                Operation = this.op
            };
        }
    }
}