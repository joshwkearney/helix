using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Primitives {
    public class BoolLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public bool Value { get; }

        public BoolLiteralSyntax(T tag, bool value) {
            this.Tag = tag;
            this.Value = value;
        }
    }
}