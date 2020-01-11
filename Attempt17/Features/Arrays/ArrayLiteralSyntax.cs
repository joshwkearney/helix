using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Arrays {
    public class ArrayLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ImmutableList<ISyntax<T>> Elements { get; }

        public ArrayLiteralSyntax(T tag, ImmutableList<ISyntax<T>> elements) {
            this.Tag = tag;
            this.Elements = elements;
        }
    }
}