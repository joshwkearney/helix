using System.Collections.Generic;
using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.FlowControl {
    public class WhileParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IParsedSyntax Condition { get; set; }

        public IParsedSyntax Body { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Condition = this.Condition.CheckNames(names);
            this.Body = this.Body.CheckNames(names);

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var cond = this.Condition.CheckTypes(names, types);
            var body = this.Body.CheckTypes(names, types);

            // Make sure the condition is a boolean
            if (types.TryUnifyTo(cond, TrophyType.Boolean).TryGetValue(out var newCond)) {
                cond = newCond;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(cond.Location, TrophyType.Boolean, cond.ReturnType);
            }

            return new WhileTypeCheckedSyntax() {
                Location = this.Location,
                Body = body,
                Condition = cond,
                Lifetimes = ImmutableHashSet.Create<IdentifierPath>(),
                ReturnType = TrophyType.Void
            };
        }
    }

    public class WhileTypeCheckedSyntax : ISyntax {
        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public ISyntax Condition { get; set; }

        public ISyntax Body { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var loopBody = new List<CStatement>();
            var writer = new CStatementWriter();

            writer.StatementWritten += (s, e) => loopBody.Add(e);

            var cond = this.Condition.GenerateCode(declWriter, writer);

            loopBody.Add(CStatement.If(CExpression.Not(cond), new[] { CStatement.Break() }));
            loopBody.Add(CStatement.NewLine());

            var body = this.Body.GenerateCode(declWriter, writer);

            statWriter.WriteStatement(CStatement.While(CExpression.IntLiteral(1), loopBody));
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.IntLiteral(0);
        }
    }
}
