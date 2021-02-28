using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class WhileSyntaxA : ISyntaxA {
        private readonly ISyntaxA cond, body;

        public TokenLocation Location { get; set; }

        public WhileSyntaxA(TokenLocation location, ISyntaxA cond, ISyntaxA body) {
            this.Location = location;
            this.cond = cond;
            this.body = body;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var cond = this.cond.CheckNames(names);
            var body = this.body.CheckNames(names);

            return new WhileSyntaxB(this.Location, cond, body);
        }
    }

    public class WhileSyntaxB : ISyntaxB {
        private readonly ISyntaxB cond, body;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.cond.VariableUsage.AddRange(body.VariableUsage);
        }

        public WhileSyntaxB(TokenLocation loc, ISyntaxB cond, ISyntaxB body) {
            this.Location = loc;
            this.cond = cond;
            this.body = body;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var cond = this.cond.CheckTypes(types);
            var body = this.body.CheckTypes(types);

            // Make sure the condition is a boolean
            if (types.TryUnifyTo(cond, TrophyType.Boolean).TryGetValue(out var newCond)) {
                cond = newCond;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, TrophyType.Boolean, cond.ReturnType);
            }

            return new WhileSyntaxC(cond, body);
        }
    }

    public class WhileSyntaxC : ISyntaxC {
        private readonly ISyntaxC cond, body;

        public TrophyType ReturnType => TrophyType.Void;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public WhileSyntaxC(ISyntaxC cond, ISyntaxC body) {
            this.cond = cond;
            this.body = body;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var loopBody = new List<CStatement>();
            var writer = new CStatementWriter();

            writer.StatementWritten += (s, e) => loopBody.Add(e);

            var cond = this.cond.GenerateCode(declWriter, writer);

            loopBody.Add(CStatement.If(CExpression.Not(cond), new[] { CStatement.Break() }));
            loopBody.Add(CStatement.NewLine());

            var body = this.body.GenerateCode(declWriter, writer);

            statWriter.WriteStatement(CStatement.Comment("While loop"));
            statWriter.WriteStatement(CStatement.While(CExpression.IntLiteral(1), loopBody));
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.IntLiteral(0);
        }
    }
}
