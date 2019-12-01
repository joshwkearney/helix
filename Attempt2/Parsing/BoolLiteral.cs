using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Parsing {
    public class BoolLiteral : IAST {
        public bool Value { get; }

        public BoolLiteral(bool value) {
            this.Value = value;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitBoolLiteral(this);
        }
    }
}