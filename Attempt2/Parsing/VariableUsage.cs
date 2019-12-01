using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Parsing {
    public class VariableUsage : IAST {
        public string VariableName { get; }

        public VariableUsage(string varName) {
            this.VariableName = varName;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitVariableUsage(this);
        }
    }
}