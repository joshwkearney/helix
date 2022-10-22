using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;

namespace Helix.Parsing {
    public partial class Parser {
        private int blockCounter = 0;

        private ISyntaxTree Block(BlockBuilder block) {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<ISyntaxTree>();

            this.scope = this.scope.Append("$block_" + this.blockCounter++);

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement(block));
            }

            this.scope = this.scope.Pop();

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            if (stats.Any()) {
                return stats.Last();
            }
            else {
                return new VoidLiteral(loc);
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BlockSyntax : ISyntaxTree {
        private static int idCounter = 0;

        private readonly int id;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.Statements;

        public IReadOnlyList<ISyntaxTree> Statements { get; }

        public bool IsPure { get; }

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntaxTree> statements,
                   bool isTypeChecked = false) {

            this.Location = location;
            this.Statements = statements;
            this.id = idCounter++;
            this.isTypeChecked = isTypeChecked;

            this.IsPure = this.Statements.All(x => x.IsPure);
        }

        public BlockSyntax(ISyntaxTree statement, bool isTypeChecked = false)
            : this(statement.Location, new[] { statement }, isTypeChecked) { }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var stats = this.Statements.Select(x => x.CheckTypes(types)).ToArray();
            var result = new BlockSyntax(this.Location, stats, true);
            var returnType = stats
                .LastOrNone()
                .Select(x => types.ReturnTypes[x])
                .OrElse(() => PrimitiveType.Void);

            types.ReturnTypes[result] = returnType;
            types.CapturedVariables[result] = stats
                .SelectMany(x => types.CapturedVariables[x])
                .ToArray();

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            if (this.Statements.Any()) {
                foreach (var stat in this.Statements.SkipLast(1)) {
                    stat.GenerateCode(writer);
                }

                return this.Statements.Last().GenerateCode(writer);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}
