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
            // Delegate type resolution
            var left = this.left.CheckTypes(types).ToRValue(types);
            var right = this.right.CheckTypes(types).ToRValue(types);

            left = left.ConvertTypeFrom(right, types);
            right = right.ConvertTypeFrom(left, types);

            var leftType = types.ReturnTypes[left];
            var rightType = types.ReturnTypes[right];
            var returnType = PrimitiveType.Void as HelixType;

            // Check if left is a valid type
            if (leftType != PrimitiveType.Int && leftType != PrimitiveType.Bool) {
                throw TypeException.UnexpectedType(this.left.Location, leftType);
            }

            // Check if right is a valid type
            if (rightType != PrimitiveType.Int && rightType != PrimitiveType.Bool) {
                throw TypeException.UnexpectedType(this.right.Location, rightType);
            }

            // Make sure this is a valid operation
            if (leftType == PrimitiveType.Int) {
                if (!intOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeException.UnexpectedType(this.left.Location, leftType);
                }

                returnType = ret;
            }
            else if (leftType == PrimitiveType.Bool) {
                if (!boolOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeException.UnexpectedType(this.left.Location, leftType);
                }

                returnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            var result = new BinarySyntax(this.Location, left, right, this.op, true);
            types.ReturnTypes[result] = returnType;

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            this.left.AnalyzeFlow(flow);
            this.right.AnalyzeFlow(flow);

            flow.Lifetimes[this] = new LifetimeBundle();
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