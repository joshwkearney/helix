using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class FunctionRecurseLiteral : ISyntaxTree {
        public Scope Scope { get; }

        public ITrophyType ExpressionType { get; }

        public bool IsConstant => true;

        public FunctionRecurseLiteral(ITrophyType returnType, Scope env) {
            this.Scope = env;
            this.ExpressionType = returnType;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}