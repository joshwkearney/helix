using Attempt17.Parsing;

namespace Attempt17.Features.Functions {
    public class FunctionDeclarationParseSyntax : ISyntax<ParseTag> {
        public ParseTag Tag { get; }

        public FunctionSignature Signature { get; }

        public ISyntax<ParseTag> Body { get; }

        public FunctionDeclarationParseSyntax(ParseTag tag, FunctionSignature signature, ISyntax<ParseTag> body) {
            this.Tag = tag;
            this.Signature = signature;
            this.Body = body;
        }
    }
}