using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class Int32Literal : ISyntax, IProtoSyntax {
        public int Value { get; }

        public ILanguageType ExpressionType => PrimitiveType.Int32Type;

        public Int32Literal(int value) {
            this.Value = value;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }

        public void Accept(IProtoSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}