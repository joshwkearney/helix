using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.FlowControl {
    public class BlockParseSyntax : IParsedSyntax {
        private static int blockCounter = 0;

        private readonly int blockId = blockCounter++;

        public TokenLocation Location { get; set; }

        public IReadOnlyList<IParsedSyntax> Statements { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            names.PushScope(names.CurrentScope.Append("$block" + this.blockId));
            this.Statements = this.Statements.Select(x => x.CheckNames(names)).ToArray();
            names.PopScope();

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            names.PushScope(names.CurrentScope.Append("$block" + this.blockId));
            var stats = this.Statements.Select(x => x.CheckTypes(names, types)).ToArray();
            names.PopScope();

            var returnType = LanguageType.Void;
            var lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            if (stats.Any()) {
                returnType = stats.Last().ReturnType;
                lifetimes = stats.Last().Lifetimes;
            }

            return new BlockTypeCheckedSyntax() {
                Location = this.Location,
                ReturnType = returnType,
                Lifetimes = lifetimes,
                Statements = stats
            };
        }
    }

    public class BlockTypeCheckedSyntax : ITypeCheckedSyntax {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public IReadOnlyList<ITypeCheckedSyntax> Statements { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            if (this.Statements.Any()) {
                foreach (var stat in this.Statements.SkipLast(1)) {
                    var expr = stat.GenerateCode(declWriter, statWriter);

                    statWriter.WriteStatement(CStatement.FromExpression(expr));
                }

                return this.Statements.Last().GenerateCode(declWriter, statWriter);
            }
            else {
                return CExpression.IntLiteral(0);
            }
        }
    }
}
