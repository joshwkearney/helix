using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new BlockSyntax(loc, stats);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public class BlockSyntax : ISyntaxTree {
        private static int idCounter = 0;

        private readonly IReadOnlyList<ISyntaxTree> statements;
        private readonly int id;

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntaxTree> statements) {
            this.Location = location;
            this.statements = statements;
            this.id = idCounter++;
        }

        public TokenLocation Location { get; }

        public Option<TrophyType> ToType(INamesObserver types) {
            return Option.None;
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            var blockName = "$block" + this.id;

            types.PushScope(blockName);
            var stats = this.statements.Select(x => x.CheckTypes(types)).ToArray();
            types.PopScope();

            var result = new BlockSyntax(this.Location, stats);
            var returnType = stats
                .LastOrNone()
                .Select(types.GetReturnType)
                .OrElse(() => PrimitiveType.Void);

            types.SetReturnType(result, returnType);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            if (this.statements.Any()) {
                foreach (var stat in this.statements.SkipLast(1)) {
                    stat.GenerateCode(writer);
                }

                return this.statements.Last().GenerateCode(writer);
            }
            else {
                return CExpression.IntLiteral(0);
            }
        }
    }
}
