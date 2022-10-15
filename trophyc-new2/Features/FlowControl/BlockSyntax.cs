using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing
{
    public partial class Parser {
        private IParseTree Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new BlockParseTree(loc, stats);
        }
    }
}

namespace Trophy.Features.FlowControl
{
    public class BlockParseTree : IParseTree {
        private static int idCounter = 0;

        private readonly IReadOnlyList<IParseTree> statements;
        private readonly int id;

        public BlockParseTree(TokenLocation location, IReadOnlyList<IParseTree> statements) {
            this.Location = location;
            this.statements = statements;
            this.id = idCounter++;
        }

        public TokenLocation Location { get; }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var blockScope = scope = scope.Append("$block" + this.id);
            var stats = this.statements.Select(x => x.ResolveTypes(blockScope, names, types)).ToArray();

            return new BlockSyntax(this.Location, this.id, stats);
        }
    }

    public class BlockSyntax : ISyntaxTree {
        private readonly IReadOnlyList<ISyntaxTree> statements;
        private readonly int id;

        public TokenLocation Location { get; }

        public TrophyType ReturnType { 
            get {
                if (this.statements.Any()) {
                    return this.statements.Last().ReturnType;
                }
                else {
                    return PrimitiveType.Void;
                }
            }
        }

        public BlockSyntax(TokenLocation location, int id, IReadOnlyList<ISyntaxTree> statements) {
            this.Location = location;
            this.id = id;
            this.statements = statements;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            if (this.statements.Any()) {
                foreach (var stat in this.statements.SkipLast(1)) {
                    stat.GenerateCode(writer, statWriter);
                }

                return this.statements.Last().GenerateCode(writer, statWriter);
            }
            else {
                return CExpression.IntLiteral(0);
            }
        }
    }
}
