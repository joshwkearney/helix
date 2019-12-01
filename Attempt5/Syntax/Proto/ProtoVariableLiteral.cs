using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class ProtoVariableLiteral : IProtoSyntax {
        public string VariableName { get; }

        public ProtoVariableLiteral(string name) {
            this.VariableName = name;
        }

        public void Accept(IProtoSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}