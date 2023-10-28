using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.FlowControl;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;
using Helix.HelixMinusMinus;

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
            { BinaryOperationKind.Add,                  PrimitiveType.Word },
            { BinaryOperationKind.Subtract,             PrimitiveType.Word },
            { BinaryOperationKind.Multiply,             PrimitiveType.Word },
            { BinaryOperationKind.Modulo,               PrimitiveType.Word },
            { BinaryOperationKind.FloorDivide,          PrimitiveType.Word },
            { BinaryOperationKind.And,                  PrimitiveType.Word },
            { BinaryOperationKind.Or,                   PrimitiveType.Word },
            { BinaryOperationKind.Xor,                  PrimitiveType.Word },
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

            var leftType = left.GetReturnType(types);
            var rightType = right.GetReturnType(types);

            if (leftType is SingularWordType singLeft && rightType is SingularWordType singRight) {
                return this.EvaluateIntExpression(singLeft.Value, singRight.Value, types);
            }

            left = left.UnifyFrom(right, types);
            right = right.UnifyFrom(left, types);

            var result = new BinarySyntax(this.Location, left, right, this.op, true);

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(left, right)
                .WithReturnType(returnType)
                .BuildFor(result);

            return result;
        }

        private ISyntaxTree EvaluateIntExpression(long int1, long int2, TypeFrame types) {
            HelixType returnType;

            switch (this.op) {
                case BinaryOperationKind.Add:
                    returnType = new SingularWordType(int1 + int2);
                    break;
                case BinaryOperationKind.Subtract:
                    returnType = new SingularWordType(int1 - int2);
                    break;
                case BinaryOperationKind.Multiply:
                    returnType = new SingularWordType(int1 * int2);
                    break;
                case BinaryOperationKind.Modulo:
                    returnType = new SingularWordType(int1 % int2);
                    break;
                case BinaryOperationKind.FloorDivide:
                    returnType = new SingularWordType(int1 / int2);
                    break;
                case BinaryOperationKind.And:
                    returnType = new SingularWordType(int1 & int2);
                    break;
                case BinaryOperationKind.Or:
                    returnType = new SingularWordType(int1 | int2);
                    break;
                case BinaryOperationKind.Xor:
                    returnType = new SingularWordType(int1 ^ int2);
                    break;
                case BinaryOperationKind.EqualTo:
                    returnType = new SingularBoolType(int1 == int2);
                    break;
                case BinaryOperationKind.NotEqualTo:
                    returnType = new SingularBoolType(int1 != int2);
                    break;
                case BinaryOperationKind.GreaterThan:
                    returnType = new SingularBoolType(int1 > int2);
                    break;
                case BinaryOperationKind.LessThan:
                    returnType = new SingularBoolType(int1 < int2);
                    break;
                case BinaryOperationKind.GreaterThanOrEqualTo:
                    returnType = new SingularBoolType(int1 >= int2);
                    break;
                case BinaryOperationKind.LessThanOrEqualTo:
                    returnType = new SingularBoolType(int1 <= int2);
                    break;
                default:
                    throw new Exception();
            }

            var result = returnType.ToSyntax(this.Location, types).GetValue();

            SyntaxTagBuilder.AtFrame(types)
                // .WithChildren(left, right) <-- Add this back??
                .WithReturnType(returnType)
                .BuildFor(result);

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

            if (leftType is SingularBoolType singLeft && rightType is SingularBoolType singRight) {
                return this.EvaluateBoolExpression(singLeft.Value, singRight.Value, predicate, types);
            }

            var result = new BinarySyntax(this.Location, left, right, this.op, true);

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(left, right)
                .WithReturnType(returnType)
                .BuildFor(result);

            return result;
        }

        private ISyntaxTree EvaluateBoolExpression(bool b1, bool b2, ISyntaxPredicate pred, TypeFrame types) {
            bool value;

            switch (this.op) {
                case BinaryOperationKind.And:
                    value = b1 & b2;
                    break;
                case BinaryOperationKind.Or:
                    value = b1 | b2;
                    break;
                case BinaryOperationKind.NotEqualTo:
                case BinaryOperationKind.Xor:
                    value = b1 ^ b2;
                    break;
                case BinaryOperationKind.EqualTo:
                    value = b1 == b2;
                    break;
                default:
                    throw new Exception();
            }

            var returnType = new SingularBoolType(value, pred);
            var result = returnType.ToSyntax(this.Location, types).GetValue();

            SyntaxTagBuilder.AtFrame(types)
                .WithReturnType(returnType)
                .BuildFor(result);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.isTypeChecked) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CBinaryExpression() {
                Left = this.left.GenerateCode(types, writer),
                Right = this.right.GenerateCode(types, writer),
                Operation = this.op
            };
        }

        public HmmValue GenerateHelixMinusMinus(TypeFrame types, HmmWriter writer) {
            var left = this.left.GenerateHelixMinusMinus(types, writer);
            var right = this.right.GenerateHelixMinusMinus(types, writer);
            var v = writer.GetTempVariable(this.GetReturnType(types));

            var stat = new BinaryStatement() {
                ResultVariable = v,
                Left = left,
                Right = right,
                Operation = this.op,
                Location = this.Location
            };

            writer.AddStatement(stat);
            return HmmValue.Variable(v);
        }
    }
}