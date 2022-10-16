using Trophy.CodeGeneration.CSyntax;

namespace Trophy.CodeGeneration {
    public class CStatementWriter : CWriter {
        private readonly IList<CStatement> stats;

        public CStatementWriter(CWriter writer, IList<CStatement> stats) {
            this.stats = stats;
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
            var name = this.GetTempVariableName();
            var stat = CStatement.VariableDeclaration(type, name, expr);

            this.WriteStatement(stat);

            return CExpression.VariableLiteral(name);
        }
    }
}
