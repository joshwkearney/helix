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
    public record BlockSyntax : ISyntaxTree {
        private static int idCounter = 0;

        private readonly IReadOnlyList<ISyntaxTree> statements;
        private readonly int id;
        private readonly bool isTypeChecked;

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntaxTree> statements, 
                           bool isTypeChecked = false) {
            this.Location = location;
            this.statements = statements;
            this.id = idCounter++;
            this.isTypeChecked = isTypeChecked;
        }

        public TokenLocation Location { get; }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            var newScope = types.CurrentScope.Append("$block" + this.id);
            types = types.WithScope(newScope);

            var stats = this.statements.Select(x => x.CheckTypes(types)).ToArray();
            var result = new BlockSyntax(this.Location, stats, true);
            var returnType = stats
                .LastOrNone()
                .Select(types.GetReturnType)
                .OrElse(() => PrimitiveType.Void);

            types.SetReturnType(result, returnType);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) {
            return this.isTypeChecked ? this : Option.None;
        }

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

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
