using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree UnaryExpression() {
            var hasOperator = this.Peek(TokenKind.Minus)
                || this.Peek(TokenKind.Plus)
                || this.Peek(TokenKind.Not)
                || this.Peek(TokenKind.Star)
                || this.Peek(TokenKind.Ampersand);

            if (hasOperator) {
                var tokOp = this.Advance();
                var first = this.SuffixExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperatorKind.Not;

                if (tokOp.Kind == TokenKind.Star) {
                    return new DereferenceSyntax(loc, first);
                }
                else if (tokOp.Kind == TokenKind.Ampersand) {
                    return new AddressOfSyntax(loc, first);
                }
                else if (tokOp.Kind == TokenKind.Plus) {
                    op = UnaryOperatorKind.Plus;
                }
                else if (tokOp.Kind == TokenKind.Minus) {
                    op = UnaryOperatorKind.Minus;
                }

                return new UnaryParseSyntax(loc, op, first);
            }

            return this.SuffixExpression();
        }
    }
}

namespace Helix.Features.Primitives {
    public enum UnaryOperatorKind {
        Not, Plus, Minus
    }

    public record UnaryParseSyntax : ISyntaxTree {
        private readonly UnaryOperatorKind op;
        private readonly ISyntaxTree arg;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { arg };

        public bool IsPure => this.arg.IsPure;

        public UnaryParseSyntax(TokenLocation location, UnaryOperatorKind op, ISyntaxTree arg) {
            this.Location = location;
            this.op = op;
            this.arg = arg;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.op == UnaryOperatorKind.Plus || this.op == UnaryOperatorKind.Minus) {
                var left = new IntLiteral(this.Location, 0);

                var op = this.op == UnaryOperatorKind.Plus 
                    ? BinaryOperationKind.Add 
                    : BinaryOperationKind.Subtract;

                var result = new BinarySyntax(this.Location, left, this.arg, op);

                return result.CheckTypes(types);
            }
            else if (this.op == UnaryOperatorKind.Not) {
                var arg = this.arg
                    .CheckTypes(types)
                    .UnifyTo(PrimitiveType.Bool, types);

                var result = new UnaryNotSyntax(
                    this.Location, 
                    arg);

                types.ReturnTypes[result] = PrimitiveType.Bool;
                types.Lifetimes[result] = new Lifetime();

                return result;
            }
            else {
                throw new Exception("Unexpected unary operator kind");
            }
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record UnaryNotSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public UnaryNotSyntax(TokenLocation loc, ISyntaxTree target) {
            this.Location = loc;
            this.target = target;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CNot() {
                Target = this.target.GenerateCode(writer)
            };
        }
    }
}