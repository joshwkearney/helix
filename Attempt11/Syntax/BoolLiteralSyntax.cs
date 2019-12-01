using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt10 {
    public class BoolLiteralSyntax : ISyntaxTree {
        public ITrophyType ExpressionType => PrimitiveTrophyType.Boolean;

        public bool Value { get; }

        public Scope Scope { get; }

        public BoolLiteralSyntax(bool value, Scope scope) {
            this.Value = value;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}