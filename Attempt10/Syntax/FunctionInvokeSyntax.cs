using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class FunctionInvokeSyntax : ISyntaxTree {
        public ISyntaxTree Target { get; }

        public Scope Scope { get; }

        public IReadOnlyList<ISyntaxTree> Arguments { get; }

        public ITrophyType ExpressionType { get; }

        public bool IsConstant { get; }

        public FunctionInvokeSyntax(ISyntaxTree target, ITrophyType returnType, Scope env, IReadOnlyList<ISyntaxTree> args) {
            this.Target = target;
            this.Scope = env;
            this.Arguments = args;
            this.ExpressionType = returnType;

            this.IsConstant = target.IsConstant && args.Aggregate(true, (x, y) => x && y.IsConstant);
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}