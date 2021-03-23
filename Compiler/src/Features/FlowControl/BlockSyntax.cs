using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class BlockSyntaxA : ISyntaxA {
        private readonly IReadOnlyList<ISyntaxA> statements;

        public BlockSyntaxA(TokenLocation location, IReadOnlyList<ISyntaxA> statements) {
            this.Location = location;
            this.statements = statements;
        }

        public TokenLocation Location { get; }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var id = names.GetNewVariableId();

            var context = names.Context.WithScope(x => x.Append("$block" + id));
            var stats = names.WithContext(context, names => {
                return this.statements.Select(x => x.CheckNames(names)).ToArray();
            });

            return new BlockSyntaxB(this.Location, id, stats);
        }
    }

    public class BlockSyntaxB : ISyntaxB {
        private readonly IReadOnlyList<ISyntaxB> statements;
        private readonly int id;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.statements
                .Select(x => x.VariableUsage)
                .Append(ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>())
                .Aggregate((x, y) => x.AddRange(y));
        }

        public BlockSyntaxB(TokenLocation location, int id, IReadOnlyList<ISyntaxB> statements) {
            this.Location = location;
            this.id = id;
            this.statements = statements;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var stats = this.statements.Select(x => x.CheckTypes(types)).ToArray();

            return new BlockSyntaxC(stats);
        }
    }

    public class BlockSyntaxC : ISyntaxC {
        private readonly IReadOnlyList<ISyntaxC> statements;

        public ITrophyType ReturnType {
            get {
                if (this.statements.Any()) {
                    return this.statements.Last().ReturnType;
                }
                else {
                    return ITrophyType.Void;
                }
            }
        }

        public ImmutableHashSet<IdentifierPath> Lifetimes {
            get {
                if (this.statements.Any()) {
                    return this.statements.Last().Lifetimes;
                }
                else {
                    return ImmutableHashSet.Create<IdentifierPath>();
                }
            }
        }

        public BlockSyntaxC(IReadOnlyList<ISyntaxC> statements) {
            this.statements = statements;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            if (this.statements.Any()) {
                foreach (var stat in this.statements.SkipLast(1)) {
                    var expr = stat.GenerateCode(declWriter, statWriter);

                    statWriter.WriteStatement(CStatement.FromExpression(expr));
                }

                return this.statements.Last().GenerateCode(declWriter, statWriter);
            }
            else {
                return CExpression.IntLiteral(0);
            }
        }
    }
}
