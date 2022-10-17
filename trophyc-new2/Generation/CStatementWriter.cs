using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Generation.Syntax;

namespace Trophy.Generation {
    public interface ICStatementWriter : ICWriter {
        public CStatementWriter WriteStatement(ICStatement stat);

        public CStatementWriter WriteEmptyLine();

        public ICSyntax WriteImpureExpression(ICSyntax type, ICSyntax expr);

        public CStatementWriter WriteComment(string comment) {
            return this.WriteStatement(new CComment(comment));
        }
    }

    public class CStatementWriter : ICStatementWriter {
        private readonly ICWriter prev;
        private readonly IList<ICStatement> stats;

        public CStatementWriter(ICWriter prev, IList<ICStatement> stats) {
            this.prev = prev;
            this.stats = stats;
        }

        public CStatementWriter WriteStatement(ICStatement stat) {
            stats.Add(stat);

            return this;
        }

        public CStatementWriter WriteEmptyLine() {
            if (this.stats.Any() && !this.stats.Last().IsEmpty) {
                this.WriteStatement(new CEmptyLine());
            }

            return this;
        }

        public ICSyntax WriteImpureExpression(ICSyntax type, ICSyntax expr) {
            var name = this.GetVariableName();

            var stat = new CVariableDeclaration() {
                Type = type,
                Name = name,
                Assignment = expr
            };

            this.WriteStatement(stat);

            return new CVariableLiteral(name);
        }

        public string GetVariableName() => this.prev.GetVariableName();

        public string GetVariableName(IdentifierPath path) => this.prev.GetVariableName(path);

        public void WriteDeclaration1(ICStatement decl) => this.prev.WriteDeclaration1(decl);

        public void WriteDeclaration2(ICStatement decl) => this.prev.WriteDeclaration2(decl);

        public void WriteDeclaration3(ICStatement decl) => this.prev.WriteDeclaration3(decl);

        public ICSyntax ConvertType(TrophyType type) => this.prev.ConvertType(type);
    }
}
