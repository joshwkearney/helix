using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree AsExpression() {
            var first = this.BinaryExpression();

            while (this.Peek(TokenKind.AsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var target = this.TopExpression();
                    var loc = first.Location.Span(this.tokens[this.pos - 1].Location);

                    first = new AsParseTree(loc, first, target);
                }
            }

            return first;
        }
    }
}

namespace Trophy.Features.Primitives {
    public record AsParseTree : ISyntaxTree {
        private readonly ISyntaxTree arg;
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public AsParseTree(TokenLocation loc, ISyntaxTree arg, ISyntaxTree target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var arg = this.arg.CheckTypes(types).ToRValue(types);

            if (!this.target.AsType(types).TryGetValue(out var targetType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.target.Location);
            }

            arg = arg.UnifyTo(targetType, types);

            return arg;
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
}