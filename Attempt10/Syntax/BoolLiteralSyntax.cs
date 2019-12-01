using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12 {
    public class BoolLiteralSyntax : ISyntaxTree {
        public ITrophyType ExpressionType => PrimitiveTrophyType.Boolean;

        public Scope Scope { get; }

        public bool Value { get; }

        public bool IsConstant => true;

        public BoolLiteralSyntax(bool value, Scope scope) {
            this.Value = value;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}