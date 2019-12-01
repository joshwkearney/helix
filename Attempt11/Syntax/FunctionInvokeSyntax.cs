using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt10 {
    public class FunctionInvokeSyntax : ISyntaxTree {
        public ISyntaxTree Target { get; }

        public IReadOnlyList<ISyntaxTree> Arguments { get; }

        public ITrophyType ExpressionType { get; }

        public FunctionInvokeSyntax(ISyntaxTree target, ITrophyType returnType, IReadOnlyList<ISyntaxTree> args) {
            this.Target = target;
            this.Arguments = args;
            this.ExpressionType = returnType;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}