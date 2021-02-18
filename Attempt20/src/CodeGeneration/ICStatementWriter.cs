using Attempt20.CodeGeneration.CSyntax;

namespace Attempt20 {
    public interface ICStatementWriter {
        public void WriteStatement(CStatement stat);
    }
}