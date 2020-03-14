using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Containers.Arrays {
    public class ArrayLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ImmutableList<ISyntax<T>> Elements { get; }

        public ArrayLiteralSyntax(T tag, ImmutableList<ISyntax<T>> elements) {
            this.Tag = tag;
            this.Elements = elements;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.ContainersVisitor.ArraysVisitor.VisitLiteral(this, visitor, context);
        }
    }
}