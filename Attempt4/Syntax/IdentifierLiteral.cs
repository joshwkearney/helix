using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt4 {
    public class IdentifierLiteral : ISyntaxTree {
        public string Value { get; }

        public IdentifierLiteral(string value) {
            this.Value = value;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}