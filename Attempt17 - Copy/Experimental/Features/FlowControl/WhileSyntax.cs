using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.FlowControl {
    public class WhileSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Condition { get; }

        public ISyntax<T> Body { get; }

        public WhileSyntax(T tag, ISyntax<T> cond, ISyntax<T> body) {
            this.Tag = tag;
            this.Condition = cond;
            this.Body = body;
        }
    }
}