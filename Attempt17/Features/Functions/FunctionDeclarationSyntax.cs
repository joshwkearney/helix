using Attempt17.TypeChecking;

namespace Attempt17.Features.Functions {
    public class FunctionDeclarationSyntax<T> : IDeclaration<T> {
        public T Tag { get; }

        public FunctionInfo FunctionInfo { get; }

        public ISyntax<T> Body { get; }

        public FunctionDeclarationSyntax(T tag, FunctionInfo info, ISyntax<T> body) {
            this.Tag = tag;
            this.FunctionInfo = info;
            this.Body = body;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.FunctionsVisitor.VisitFunctionDeclaration(this, visitor, context);
        }

        public T1 Accept<T1>(IDeclarationVisitor<T1, T> visitor, ITypeCheckScope scope) {
            return visitor.VisitFunctionDeclaration(this, scope);
        }
    }
}