using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Variables;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree UnaryExpression() {
            var hasOperator = this.Peek(TokenKind.Minus)
                || this.Peek(TokenKind.Plus)
                || this.Peek(TokenKind.Not)
                || this.Peek(TokenKind.Ampersand);

            if (hasOperator) {
                var tokOp = this.Advance();
                var first = this.UnaryExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperatorKind.Not;

                if (tokOp.Kind == TokenKind.Plus) {
                    op = UnaryOperatorKind.Plus;
                }
                else if (tokOp.Kind == TokenKind.Minus) {
                    op = UnaryOperatorKind.Minus;
                }
                else if (tokOp.Kind == TokenKind.Ampersand) {
                    return new AddressOfSyntax(loc, first);
                }
                else if (tokOp.Kind != TokenKind.Not) {
                    throw new Exception("Unexpected unary operator");
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

        public IEnumerable<ISyntaxTree> Children => new[] { this.arg };

        public bool IsPure => this.arg.IsPure;

        public UnaryParseSyntax(TokenLocation location, UnaryOperatorKind op, ISyntaxTree arg) {
            this.Location = location;
            this.op = op;
            this.arg = arg;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.op == UnaryOperatorKind.Plus || this.op == UnaryOperatorKind.Minus) {
                var left = new WordLiteral(this.Location, 0);

                var op = this.op == UnaryOperatorKind.Plus
                    ? BinaryOperationKind.Add
                    : BinaryOperationKind.Subtract;

                var result = new BinarySyntax(this.Location, left, this.arg, op);

                return result.CheckTypes(types);
            }
            else if (this.op == UnaryOperatorKind.Not) {
                var arg = this.arg.CheckTypes(types);
                var returnType = arg.GetReturnType(types);

                if (returnType is PredicateBool pred) {
                    returnType = new PredicateBool(pred.Predicate.Negate());
                }
                else {
                    arg = arg.UnifyTo(PrimitiveType.Bool, types);
                    returnType = PrimitiveType.Bool;
                }

                var result = new UnaryNotSyntax(
                    this.Location,
                    arg);

                SyntaxTagBuilder.AtFrame(types)
                    .WithChildren(arg)
                    .WithReturnType(returnType)
                    .BuildFor(result);

                return result;
            }
            else {
                throw new Exception("Unexpected unary operator kind");
            }
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

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CNot() {
                Target = this.target.GenerateCode(types, writer)
            };
        }

        public HmmValue GenerateHelixMinusMinus(TypeFrame types, HmmWriter writer) {
            var value = this.target.GenerateHelixMinusMinus(types, writer);
            var v = writer.GetTempVariable(this.GetReturnType(types));

            var stat = new UnaryStatement() {
                ResultVariable = v,
                Operand = value,
                Operation = UnaryOperatorKind.Not,
                Location = this.Location
            };

            writer.AddStatement(stat);
            return HmmValue.Variable(v);
        }
    }
}