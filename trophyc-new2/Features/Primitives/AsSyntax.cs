using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax AsExpression() {
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
    public record AsParseTree : ISyntax {
        private readonly ISyntax arg;
        private readonly ISyntax target;

        public TokenLocation Location { get; }

        public AsParseTree(TokenLocation loc, ISyntax arg, ISyntax target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            var arg = this.arg.CheckTypes(types).ToRValue(types);

            if (!this.target.AsType(types).TryGetValue(out var targetType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.target.Location);
            }

            arg = arg.UnifyTo(targetType, types);

            return arg;
        }

        public ISyntax ToRValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}