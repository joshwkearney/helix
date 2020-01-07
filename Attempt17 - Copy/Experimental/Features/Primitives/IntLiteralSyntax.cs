using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Primitives {
    public class IntLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public long Value { get; }

        public IntLiteralSyntax(T tag, long value) {
            this.Tag = tag;
            this.Value = value;
        }
    }
}