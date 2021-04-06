using Trophy.CodeGeneration.CSyntax;
using System;

namespace Trophy.CodeGeneration {
    public class CStatementWriter : ICStatementWriter {
        public event EventHandler<CStatement> StatementWritten;

        public void WriteStatement(CStatement stat) {
            this.StatementWritten?.Invoke(this, stat);
        }
    }
}
