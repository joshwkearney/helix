using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class ProtoVariableStore : IProtoSyntax {
        public string VariableName { get; }

        public IProtoSyntax AssignExpression { get; }

        public ProtoVariableStore(string name, IProtoSyntax assign) {
            this.VariableName = name;
            this.AssignExpression = assign;
        }

        public void Accept(IProtoSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}