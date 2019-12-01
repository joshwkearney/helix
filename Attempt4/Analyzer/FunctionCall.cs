using System.Collections.Generic;

namespace Attempt4 {
    public class FunctionCall : IAnalyzedSyntax {
        public IntrinsicFunction Target { get; }

        public IReadOnlyList<IAnalyzedSyntax> Arguments { get; }

        public LanguageType ExpressionType => this.Target.ExpressionType;

        public FunctionCall(IntrinsicFunction target, IReadOnlyList<IAnalyzedSyntax> args) {
            this.Target = target;
            this.Arguments = args;
        }

        public void Accept(IAnalyzedSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}