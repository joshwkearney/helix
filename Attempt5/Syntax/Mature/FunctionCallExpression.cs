using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Syntax {
    public class FunctionCallExpression : ISyntax {
        public IFunctionSyntax Target { get; }

        public IReadOnlyList<ISyntax> Arguments { get; }

        public ILanguageType ExpressionType => this.Target.ExpressionType.ReturnType;

        public FunctionCallExpression(IFunctionSyntax target, IEnumerable<ISyntax> paras) {
            this.Target = target;
            this.Arguments = paras.ToArray();
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}