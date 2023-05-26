using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;

namespace Helix.Parsing {
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

namespace Helix.Features.FlowControl {
    public record BlockSyntax : ISyntaxTree {
        private static int blockCounter = 0;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.Statements;

        public IReadOnlyList<ISyntaxTree> Statements { get; }

        public bool IsPure { get; }

        public string Name { get; }

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntaxTree> statements) {
            this.Location = statements.Select(x => x.Location).Prepend(location).Last();
            this.Statements = statements;
            this.IsPure = this.Statements.All(x => x.IsPure);
            this.Name = "$b" + blockCounter++;
        }

        public BlockSyntax(ISyntaxTree statement) : this(statement.Location, new[] { statement }) { }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            // TODO: Fix predicates here
            types = new TypeFrame(types, this.Name);
            var stats = this.Statements.Select(x => x.CheckTypes(types).ToRValue(types)).ToArray();

            var result = new BlockSyntax(this.Location, stats);
            var returnType = stats
                .LastOrNone()
                .Select(x => types.ReturnTypes[x])
                .OrElse(() => PrimitiveType.Void);

            result.SetReturnType(returnType, types);
            result.SetCapturedVariables(stats, types);
            result.SetPredicate(ISyntaxPredicate.Empty, types);

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            foreach (var stat in this.Statements) {
                stat.AnalyzeFlow(flow);
            }

            var bundle = this.Statements
                .LastOrNone()
                .Select(x => x.GetLifetimes(flow))
                .OrElse(() => new LifetimeBundle());

            this.SetLifetimes(bundle, flow);
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            if (this.Statements.Any()) {
                foreach (var stat in this.Statements.SkipLast(1)) {
                    stat.GenerateCode(types, writer);
                }

                return this.Statements.Last().GenerateCode(types, writer);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}
