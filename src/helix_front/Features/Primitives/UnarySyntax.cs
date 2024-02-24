using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Variables;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree UnaryExpression() {
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

    public record UnaryParseSyntax(
        TokenLocation Location,
        IParseTree Operand, 
        UnaryOperatorKind Operator) : IParseTree {

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            if (this.Operator == UnaryOperatorKind.Plus || this.Operator == UnaryOperatorKind.Minus) {
                var op = this.Operator == UnaryOperatorKind.Plus
                    ? BinaryOperationKind.Add
                    : BinaryOperationKind.Subtract;

                var syntax = new BinarySyntax(
                    this.Location, 
                    new WordLiteral(this.Location, 0), 
                    this.Operand, 
                    op);

                return syntax.ToImperativeSyntax(writer);
            }
            else {
                var syntax = new BinarySyntax(
                    this.Location,
                    new BoolLiteral(this.Location, false),
                    this.Operand,
                    BinaryOperationKind.Xor);

                return syntax.ToImperativeSyntax(writer);
            }
        }
    }
}