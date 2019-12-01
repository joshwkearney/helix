using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class ProtoStatement : IProtoSyntax {
        public IProtoSyntax StatementExpression { get; }

        public IProtoSyntax ReturnExpression { get; }

        public ProtoStatement(IProtoSyntax stat, IProtoSyntax ret) {
            this.ReturnExpression = ret;
            this.StatementExpression = stat;
        }

        public void Accept(IProtoSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}