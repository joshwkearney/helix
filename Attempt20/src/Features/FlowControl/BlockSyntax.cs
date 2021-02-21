using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.FlowControl {
    public class BlockSyntaxA : ISyntaxA {
        private readonly IReadOnlyList<ISyntaxA> statements;

        public BlockSyntaxA(TokenLocation location, IReadOnlyList<ISyntaxA> statements) {
            this.Location = location;
            this.statements = statements;
        }

        public TokenLocation Location { get; }

        public ISyntaxB CheckNames(INameRecorder names) {
            var id = names.GetNewVariableId();

            names.PushScope(names.CurrentScope.Append("$block" + id));
            var stats = this.statements.Select(x => x.CheckNames(names)).ToArray();
            names.PopScope();

            return new BlockSyntaxB(this.Location, id, stats);
        }
    }

    public class BlockSyntaxB : ISyntaxB {
        private readonly IReadOnlyList<ISyntaxB> statements;
        private readonly int id;

        public TokenLocation Location { get; }

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

        public TrophyType ReturnType {
            get {
                if (this.statements.Any()) {
                    return this.statements.Last().ReturnType;
                }
                else {
                    return TrophyType.Void;
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
