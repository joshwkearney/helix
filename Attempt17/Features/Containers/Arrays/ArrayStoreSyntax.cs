using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Containers.Arrays {
    public class ArrayStoreSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ISyntax<T> Index { get; }

        public ISyntax<T> Value { get; }

        public ArrayStoreSyntax(T tag, ISyntax<T> target, ISyntax<T> index, ISyntax<T> value) {
            this.Tag = tag;
            this.Target = target;
            this.Index = index;
            this.Value = value;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.ContainersVisitor.ArraysVisitor.VisitStore(this, visitor, context);
        }
    }
}