using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree UnaryExpression() {
            if (this.Peek(TokenKind.Subtract) || this.Peek(TokenKind.Add) || this.Peek(TokenKind.Not)) {
                var tokOp = this.Advance();
                var first = this.SuffixExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperatorKind.Not;

                if (tokOp.Kind == TokenKind.Add) {
                    op = UnaryOperatorKind.Plus;
                }
                else if (tokOp.Kind == TokenKind.Subtract) {
                    op = UnaryOperatorKind.Minus;
                }

                return new UnaryParseSyntax(loc, op, first);
            }

            return this.SuffixExpression();
        }
    }
}

namespace Trophy.Features.Primitives {
    public enum UnaryOperatorKind {
        Not, Plus, Minus
    }

    public class UnaryParseSyntax : ISyntaxTree {
        private readonly UnaryOperatorKind op;
        private readonly ISyntaxTree arg;

        public TokenLocation Location { get; }

        public UnaryParseSyntax(TokenLocation location, UnaryOperatorKind op, ISyntaxTree arg) {
            this.Location = location;
            this.op = op;
            this.arg = arg;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            if (this.op == UnaryOperatorKind.Plus || this.op == UnaryOperatorKind.Minus) {
                var left = new IntLiteral(this.Location, 0);

                var op = this.op == UnaryOperatorKind.Plus 
                    ? BinaryOperationKind.Add 
                    : BinaryOperationKind.Subtract;

                var result = new BinarySyntax(this.Location, left, this.arg, op);

                return result.ResolveTypes(scope, types);
            }
            else if (this.op == UnaryOperatorKind.Not) {
                var result = new BinarySyntax(
                    this.Location, 
                    new BoolLiteral(this.Location, true), 
                    this.arg, 
                    BinaryOperationKind.Xor);

                return result.ResolveTypes(scope, types);
            }
            else {
                throw new Exception("Unexpected unary operator kind");
            }
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) {
            throw new InvalidOperationException();
        }

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) {
            throw new InvalidOperationException();
        }

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }
}