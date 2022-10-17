using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Analysis.Unification;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, BinaryOperationKind.Or);
            }

            return first;
        }

        private ISyntaxTree XorExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.ComparisonExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, BinaryOperationKind.Xor);
            }

            return first;
        }

        private ISyntaxTree ComparisonExpression() {
            var first = this.AndExpression();
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
                var second = this.AndExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxTree AndExpression() {
            var first = this.AddExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var second = this.AddExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, BinaryOperationKind.And);
            }

            return first;
        }

        private ISyntaxTree AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.Add) && !this.Peek(TokenKind.Subtract)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.Add ? BinaryOperationKind.Add : BinaryOperationKind.Subtract;
                var second = this.MultiplyExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxTree MultiplyExpression() {
            var first = this.PrefixExpression();

            while (true) {
                if (!this.Peek(TokenKind.Multiply) && !this.Peek(TokenKind.Modulo) && !this.Peek(TokenKind.Divide)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = BinaryOperationKind.Modulo;

                if (tok == TokenKind.Multiply) {
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

namespace Trophy.Features.Primitives {
    public enum BinaryOperationKind {
        Add, Subtract, Multiply, Modulo, FloorDivide,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public record BinarySyntax : ISyntaxTree {
        private static readonly Dictionary<BinaryOperationKind, TrophyType> intOperations = new() {
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

        private static readonly Dictionary<BinaryOperationKind, TrophyType> boolOperations = new() {
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

        public BinarySyntax(TokenLocation loc, ISyntaxTree left, ISyntaxTree right, 
                            BinaryOperationKind op, bool isTypeChecked = false) {
            this.Location = loc;
            this.left = left;
            this.right = right;
            this.op = op;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            // Delegate type resolution
            var left = this.left.CheckTypes(types);
            var right = this.right.CheckTypes(types);

            var leftType = types.GetReturnType(left);
            var rightType = types.GetReturnType(right);
            var returnType = PrimitiveType.Void as TrophyType;

            // Check if left is a valid type
            if (leftType != PrimitiveType.Int && leftType != PrimitiveType.Bool) {
                throw TypeCheckingErrors.UnexpectedType(this.left.Location, leftType);
            }

            // Check if right is a valid type
            if (rightType != PrimitiveType.Int && rightType != PrimitiveType.Bool) {
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, rightType);
            }

            // Make sure types match
            if (types.TryUnifyTo(right, rightType, leftType).TryGetValue(out var newRight)) { 
                right = newRight;
                rightType = leftType;
            }
            else if (types.TryUnifyTo(left, leftType, rightType).TryGetValue(out var newLeft)) {
                left = newLeft;
                leftType = rightType;
            }
            else { 
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, leftType, rightType);
            }

            // Make sure this is a valid operation
            if (leftType == PrimitiveType.Int) {
                if (!intOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(this.left.Location, leftType);
                }

                returnType = ret;
            }
            else if (leftType == PrimitiveType.Bool) {
                if (!boolOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(this.left.Location, leftType);
                }

                returnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            var result = new BinarySyntax(this.Location, left, right, this.op, true);
            types.SetReturnType(result, returnType);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) {
            return this.isTypeChecked ? this : Option.None;
        }

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CBinaryExpression() {
                Left = this.left.GenerateCode(writer),
                Right = this.right.GenerateCode(writer),
                Operation = this.op
            };
        }
    }
}