using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.FlowControl;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<ISyntax>();

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
    public record BlockSyntax : ISyntax {
        private static int idCounter = 0;

        private readonly IReadOnlyList<ISyntax> statements;
        private readonly int id;
        private readonly bool isTypeChecked;

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntax> statements, 
                           bool isTypeChecked = false) {
            this.Location = location;
            this.statements = statements;
            this.id = idCounter++;
            this.isTypeChecked = isTypeChecked;
        }

        public TokenLocation Location { get; }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
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

        public ISyntax ToRValue(ITypesRecorder types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
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
                return new CIntLiteral(0);
            }
        }
    }
}
