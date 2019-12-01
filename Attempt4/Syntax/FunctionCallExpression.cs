using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt4 {
    public class FunctionCallExpression : ISyntaxTree {
        public ISyntaxTree Target { get; }

        public IReadOnlyList<ISyntaxTree> Arguments { get; }

        public FunctionCallExpression(ISyntaxTree target, params ISyntaxTree[] args) {
            this.Target = target;
            this.Arguments = args;
        }

        public FunctionCallExpression(ISyntaxTree target, IReadOnlyList<ISyntaxTree> args) {
            this.Target = target;
            this.Arguments = args;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}