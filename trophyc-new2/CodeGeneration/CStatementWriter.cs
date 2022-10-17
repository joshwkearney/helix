using Trophy.Analysis;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.CodeGeneration {
    public class CStatementWriter : CWriter {
        private readonly IList<CStatement> stats;

        public CStatementWriter(IList<CStatement> stats) {
            this.stats = stats;
        }

        public CStatementWriter(IList<CStatement> stats, IdentifierPath scope) {
            this.stats = stats;
            this.CurrentScope = scope;
        }

        public CStatementWriter WriteStatement(CStatement stat) {
            stats.Add(stat);

            return this;
        }

        public CStatementWriter WriteSpacingLine() {
            if (this.stats.Any() && !this.stats.Last().IsEmpty) {
                this.WriteStatement(CStatement.NewLine());
            }

            return this;
        }

        public CExpression WriteImpureExpression(CType type, CExpression expr) {
            var name = this.GetVariableName();
            var stat = CStatement.VariableDeclaration(type, name, expr);

            this.WriteStatement(stat);

            return CExpression.VariableLiteral(name);
        }
    }
}
