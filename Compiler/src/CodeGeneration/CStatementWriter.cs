using Attempt20.CodeGeneration.CSyntax;
using System;

namespace Attempt20.CodeGeneration {
    public class CStatementWriter : ICStatementWriter {
        public event EventHandler<CStatement> StatementWritten;

        public void WriteStatement(CStatement stat) {
            this.StatementWritten?.Invoke(this, stat);
        }
    }
}
