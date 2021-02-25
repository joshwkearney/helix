using Trophy.CodeGeneration.CSyntax;

namespace Trophy {
    public interface ICStatementWriter {
        public void WriteStatement(CStatement stat);
    }
}