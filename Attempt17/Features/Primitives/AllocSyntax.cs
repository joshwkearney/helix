using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Primitives {
    public class AllocSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public AllocSyntax(T tag, ISyntax<T> target) {
            this.Tag = tag;
            this.Target = target;
        }
    }
}