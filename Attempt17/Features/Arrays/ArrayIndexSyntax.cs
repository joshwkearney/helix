using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Arrays {
    public class ArrayIndexSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ISyntax<T> Index { get; }

        public ArrayIndexSyntax(T tag, ISyntax<T> target, ISyntax<T> index) {
            this.Tag = tag;
            this.Target = target;
            this.Index = index;
        }
    }
}