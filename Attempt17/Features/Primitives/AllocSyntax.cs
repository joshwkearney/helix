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

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.PrimitivesVisitor.VisitAlloc(this, visitor, context);
        }
    }
}