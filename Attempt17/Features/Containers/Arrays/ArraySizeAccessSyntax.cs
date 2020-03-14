using System;
using System.Collections.Generic;
using System.Text;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Arrays {
    public class ArraySizeAccessSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ArraySizeAccessSyntax(T tag, ISyntax<T> target) {
            this.Tag = tag;
            this.Target = target;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.ContainersVisitor.ArraysVisitor.VisitSizeAccess(this, visitor, context);
        }
    }
}