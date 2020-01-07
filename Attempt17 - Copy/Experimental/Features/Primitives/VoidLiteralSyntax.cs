using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Primitives {
    public class VoidLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public VoidLiteralSyntax(T tag) {
            this.Tag = tag;
        }
    }
}