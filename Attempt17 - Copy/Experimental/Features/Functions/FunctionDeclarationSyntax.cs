using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Functions {
    public class FunctionDeclarationSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public FunctionSignature Signature { get; }

        public ISyntax<T> Body { get; }

        public FunctionDeclarationSyntax(T tag, FunctionSignature signature, ISyntax<T> body) {
            this.Tag = tag;
            this.Signature = signature;
            this.Body = body;
        }
    }
}