using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Containers.Arrays {
    public class ArrayIndexSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ISyntax<T> Index { get; }

        public ArrayIndexSyntax(T tag, ISyntax<T> target, ISyntax<T> index) {
            this.Tag = tag;
            this.Target = target;
            this.Index = index;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.ContainersVisitor.ArraysVisitor.VisitIndex(this, visitor, context);
        }
    }
}