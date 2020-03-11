using Attempt17.TypeChecking;

namespace Attempt17.Features.Functions {
    public class FunctionDeclarationSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public FunctionInfo FunctionInfo { get; }

        public ISyntax<T> Body { get; }

        public FunctionDeclarationSyntax(T tag, FunctionInfo info, ISyntax<T> body) {
            this.Tag = tag;
            this.FunctionInfo = info;
            this.Body = body;
        }
    }
}