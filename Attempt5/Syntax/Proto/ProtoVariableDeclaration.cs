using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class ProtoVariableDeclaration : IProtoSyntax {
        public string Name { get; }

        public IProtoSyntax AssignExpression { get; }

        public IProtoSyntax ScopeExpression { get; }

        public bool IsReadOnly { get; }

        public ProtoVariableDeclaration(string name, bool isreadonly, IProtoSyntax assign, IProtoSyntax scope) {
            this.Name = name;
            this.AssignExpression = assign;
            this.ScopeExpression = scope;
            this.IsReadOnly = isreadonly;
        }

        public void Accept(IProtoSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}