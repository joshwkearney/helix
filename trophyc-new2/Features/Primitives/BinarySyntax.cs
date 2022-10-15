using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing
{
    public partial class Parser {
        private IParseTree OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseTree(loc, first, second, BinaryOperation.Or);
            }

            return first;
        }

        private IParseTree XorExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.ComparisonExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseTree(loc, first, second, BinaryOperation.Xor);
            }

            return first;
        }

        private IParseTree ComparisonExpression() {
            var first = this.AndExpression();
            var comparators = new Dictionary<TokenKind, BinaryOperation>() {
                { TokenKind.Equals, BinaryOperation.EqualTo }, { TokenKind.NotEquals, BinaryOperation.NotEqualTo },
                { TokenKind.LessThan, BinaryOperation.LessThan }, { TokenKind.GreaterThan, BinaryOperation.GreaterThan },
                { TokenKind.LessThanOrEqualTo, BinaryOperation.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualTo, BinaryOperation.GreaterThanOrEqualTo }
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

                first = new BinaryParseTree(loc, first, second, op);
            }

            return first;
        }

        private IParseTree AndExpression() {
            var first = this.AddExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var second = this.AddExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseTree(loc, first, second, BinaryOperation.And);
            }

            return first;
        }

        private IParseTree AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.Add) && !this.Peek(TokenKind.Subtract)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.Add ? BinaryOperation.Add : BinaryOperation.Subtract;
                var second = this.MultiplyExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseTree(loc, first, second, op);
            }

            return first;
        }

        private IParseTree MultiplyExpression() {
            var first = this.PrefixExpression();

            while (true) {
                if (!this.Peek(TokenKind.Multiply) && !this.Peek(TokenKind.Modulo) && !this.Peek(TokenKind.Divide)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = BinaryOperation.Modulo;

                if (tok == TokenKind.Multiply) {
                    op = BinaryOperation.Multiply;
                }
                else if (tok == TokenKind.Divide) {
                    op = BinaryOperation.FloorDivide;
                }

                var second = this.PrefixExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseTree(loc, first, second, op);
            }

            return first;
        }
    }
}

namespace Trophy.Features.Primitives
{
    public enum BinaryOperation {
        Add, Subtract, Multiply, Modulo, FloorDivide,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public class BinaryParseTree : IParseTree {
        private static readonly Dictionary<BinaryOperation, TrophyType> intOperations = new() {
            { BinaryOperation.Add,                  PrimitiveType.Int },
            { BinaryOperation.Subtract,             PrimitiveType.Int },
            { BinaryOperation.Multiply,             PrimitiveType.Int },
            { BinaryOperation.Modulo,               PrimitiveType.Int },
            { BinaryOperation.FloorDivide,          PrimitiveType.Int },
            { BinaryOperation.And,                  PrimitiveType.Int },
            { BinaryOperation.Or,                   PrimitiveType.Int },
            { BinaryOperation.Xor,                  PrimitiveType.Int },
            { BinaryOperation.EqualTo,              PrimitiveType.Bool },
            { BinaryOperation.NotEqualTo,           PrimitiveType.Bool },
            { BinaryOperation.GreaterThan,          PrimitiveType.Bool },
            { BinaryOperation.LessThan,             PrimitiveType.Bool },
            { BinaryOperation.GreaterThanOrEqualTo, PrimitiveType.Bool },
            { BinaryOperation.LessThanOrEqualTo,    PrimitiveType.Bool },
        };

        private static readonly Dictionary<BinaryOperation, TrophyType> boolOperations = new() {
            { BinaryOperation.And,                  PrimitiveType.Bool },
            { BinaryOperation.Or,                   PrimitiveType.Bool },
            { BinaryOperation.Xor,                  PrimitiveType.Bool },
            { BinaryOperation.EqualTo,              PrimitiveType.Bool },
            { BinaryOperation.NotEqualTo,           PrimitiveType.Bool },
        };

        private readonly IParseTree left, right;
        private readonly BinaryOperation op;

        public TokenLocation Location { get; }

        public BinaryParseTree(TokenLocation loc, IParseTree left, IParseTree right, BinaryOperation op) {
            this.Location = loc;
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            // Delegate type resolution
            var left = this.left.ResolveTypes(scope, names, types);
            var right = this.right.ResolveTypes(scope, names, types);
            var returnType = PrimitiveType.Void as TrophyType;

            // Check if left is a valid type
            if (left.ReturnType != PrimitiveType.Int && left.ReturnType != PrimitiveType.Bool) {
                throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
            }

            // Check if right is a valid type
            if (right.ReturnType != PrimitiveType.Int && right.ReturnType != PrimitiveType.Bool) {
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, right.ReturnType);
            }

            // Make sure types match
            if (right.TryUnifyTo(left.ReturnType).TryGetValue(out var newRight)) {
                right = newRight;
            }
            else if (left.TryUnifyTo(right.ReturnType).TryGetValue(out var newLeft)) {
                left = newLeft;
            }
            else { 
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, left.ReturnType, right.ReturnType);
            }

            // Make sure this is a valid operation
            if (left.ReturnType == PrimitiveType.Int) {
                if (!intOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
                }

                returnType = ret;
            }
            else if (left.ReturnType == PrimitiveType.Bool) {
                if (!boolOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
                }

                returnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            return new BinarySyntax(left, right, this.op, returnType);
        }
    }

    public class BinarySyntax : ISyntaxTree {
        private readonly ISyntaxTree left, right;
        private readonly BinaryOperation op;

        public TrophyType ReturnType { get; }

        public BinarySyntax(ISyntaxTree left, ISyntaxTree right, BinaryOperation op, TrophyType returnType) {
            this.left = left;
            this.right = right;
            this.op = op;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var left = this.left.GenerateCode(writer, statWriter);
            var right = this.right.GenerateCode(writer, statWriter);
            var bin = CExpression.BinaryExpression(left, right, this.op);

            return bin;
        }
    }
}