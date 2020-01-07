using Attempt17.TypeChecking;

namespace Attempt17.Features.Functions {
    public class FunctionDeclarationSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public FunctionInfo Info { get; }

        public ISyntax<TypeCheckTag> Body { get; }

        public FunctionDeclarationSyntax(TypeCheckTag tag, FunctionInfo info, ISyntax<TypeCheckTag> body) {
            this.Tag = tag;
            this.Info = info;
            this.Body = body;
        }
    }
}