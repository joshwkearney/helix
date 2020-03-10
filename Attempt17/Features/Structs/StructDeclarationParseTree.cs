using System;
using Attempt17.Parsing;

namespace Attempt17.Features.Structs {
    public class StructDeclarationParseTree : ISyntax<ParseTag> {
        public ParseTag Tag { get; }

        public StructSignature Signature { get; }

        public StructDeclarationParseTree(ParseTag tag, StructSignature sig) {
            this.Tag = tag;
            this.Signature = sig;
        }
    }
}