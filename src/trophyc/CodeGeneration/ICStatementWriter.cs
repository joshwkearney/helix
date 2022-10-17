using Trophy.Generation.CSyntax;

namespace Trophy {
    public interface ICStatementWriter {
        public void WriteStatement(CStatement stat);
    }
}