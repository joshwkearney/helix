using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

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
    public class AsParseTree : ISyntaxTree {
        private readonly ISyntaxTree arg;
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public AsParseTree(TokenLocation loc, ISyntaxTree arg, ISyntaxTree target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;
        }

        public Option<TrophyType> ToType(INamesObserver types) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            if (!this.arg.CheckTypes(types).ToRValue(types).TryGetValue(out var arg)) {
                throw TypeCheckingErrors.RValueRequired(this.arg.Location);
            }

            var argType = types.GetReturnType(arg);

            if (!this.target.ToType(types).TryGetValue(out var targetType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.target.Location);
            }

            if (!TypeUnifier.TryUnifyTo(arg, argType, targetType).TryGetValue(out arg)) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, targetType, argType);
            }

            return arg;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public CExpression GenerateCode(CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }
}