using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;

namespace Trophy.Generation {
    public interface ICStatementWriter : ICWriter {
        public CStatementWriter WriteStatement(CStatement stat);

        public CStatementWriter WriteEmptyLine();

        public CExpression WriteImpureExpression(CType type, CExpression expr);
    }

    public class CStatementWriter : ICStatementWriter {
        private readonly ICWriter prev;
        private readonly IList<CStatement> stats;

        public CStatementWriter(ICWriter prev, IList<CStatement> stats) {
            this.prev = prev;
            this.stats = stats;
        }

        public CStatementWriter WriteStatement(CStatement stat) {
            stats.Add(stat);

            return this;
        }

        public CStatementWriter WriteEmptyLine() {
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

        public string GetVariableName() => this.prev.GetVariableName();

        public string GetVariableName(IdentifierPath path) => this.prev.GetVariableName(path);

        public void WriteDeclaration1(CDeclaration decl) => this.prev.WriteDeclaration1(decl);

        public void WriteDeclaration2(CDeclaration decl) => this.prev.WriteDeclaration2(decl);

        public void WriteDeclaration3(CDeclaration decl) => this.prev.WriteDeclaration3(decl);

        public CType ConvertType(TrophyType type) => this.prev.ConvertType(type);
    }
}
