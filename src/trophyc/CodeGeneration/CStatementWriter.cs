using Trophy.Generation.CSyntax;
using System;

namespace Trophy.Generation {
    public class CStatementWriter : ICStatementWriter {
        public event EventHandler<CStatement> StatementWritten;

        public void WriteStatement(CStatement stat) {
            this.StatementWritten?.Invoke(this, stat);
        }
    }
}
