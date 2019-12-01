using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Parsing {
    public class Int32Literal : IAST {
        public int Value { get; }

        public Int32Literal(int value) {
            this.Value = value;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitInt32Literal(this);
        }
    }
}