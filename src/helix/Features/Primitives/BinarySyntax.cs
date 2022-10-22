using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.FlowControl;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree OrExpression(BlockBuilder block) {
            var first = this.XorExpression(block);

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var branching = this.TryAdvance(TokenKind.ElseKeyword);
                var second = this.XorExpression(block);
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    var ifId = this.scope.Append(block.GetTempName());

                    var test = new IfParseSyntax(loc, ifId, first,
                        new BlockSyntax(new SetIfBranchSyntax(loc, ifId, true, new BoolLiteral(loc, true))),
                        new BlockSyntax(new SetIfBranchSyntax(loc, ifId, false, second)));

                    block.Statements.Add(test);
                    first = new IfAccessSyntax(loc, ifId);
                }
                else {
                    first = new BinarySyntax(loc, first, second, BinaryOperationKind.Or);
                }
            }

            return first;
        }

        private ISyntaxTree XorExpression(BlockBuilder block) {
            var first = this.AndExpression(block);

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.AndExpression(block);
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, BinaryOperationKind.Xor);
            }

            return first;
        }

        private ISyntaxTree AndExpression(BlockBuilder block) {
            var first = this.ComparisonExpression(block);

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var branching = this.TryAdvance(TokenKind.ThenKeyword);
                if (branching) {
                    var (stats, retExpr) = this.TopBlock();
                    stats.Add(retExpr);

                    var loc = first.Location.Span(retExpr.Location);
                    var ifId = this.scope.Append(block.GetTempName());

                    var test = new IfParseSyntax(loc, ifId, first,
                        new BlockSyntax(new SetIfBranchSyntax(loc, ifId, true, new BlockSyntax(loc, stats))),
                        new BlockSyntax(new SetIfBranchSyntax(loc, ifId, false, new BoolLiteral(loc, false))));

                    block.Statements.Add(test);
                    first = new IfAccessSyntax(loc, ifId);
                }
                else {
                    var second = this.ComparisonExpression(block);
                    var loc = first.Location.Span(second.Location);

                    first = new BinarySyntax(loc, first, second, BinaryOperationKind.And);
                }
            }

            return first;
        }

        private ISyntaxTree ComparisonExpression(BlockBuilder block) {
            var first = this.AddExpression(block);
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
                var second = this.AddExpression(block);
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }


        private ISyntaxTree AddExpression(BlockBuilder block) {
            var first = this.MultiplyExpression(block);

            while (true) {
                if (!this.Peek(TokenKind.Plus) && !this.Peek(TokenKind.Minus)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.Plus ? BinaryOperationKind.Add : BinaryOperationKind.Subtract;
                var second = this.MultiplyExpression(block);
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxTree MultiplyExpression(BlockBuilder block) {
            var first = this.PrefixExpression(block);

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

                var second = this.PrefixExpression(block);
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

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            // Delegate type resolution
            var left = this.left.CheckTypes(types).ToRValue(types);
            var right = this.right.CheckTypes(types).ToRValue(types);

            left = left.UnifyFrom(right, types);
            right = right.UnifyFrom(left, types);

            var leftType = types.ReturnTypes[left];
            var rightType = types.ReturnTypes[right];
            var returnType = PrimitiveType.Void as HelixType;

            // Check if left is a valid type
            if (leftType != PrimitiveType.Int && leftType != PrimitiveType.Bool) {
                throw TypeCheckingErrors.UnexpectedType(this.left.Location, leftType);
            }

            // Check if right is a valid type
            if (rightType != PrimitiveType.Int && rightType != PrimitiveType.Bool) {
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, rightType);
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
            types.ReturnTypes[result] = returnType;

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CBinaryExpression() {
                Left = this.left.GenerateCode(types, writer),
                Right = this.right.GenerateCode(types, writer),
                Operation = this.op
            };
        }
    }
}