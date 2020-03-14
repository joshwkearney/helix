using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Containers.Arrays {
    public class ArrayRangeLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public LanguageType ElementType { get; }

        public ISyntax<T> ElementCount { get; }

        public ArrayRangeLiteralSyntax(T tag, LanguageType elementType, ISyntax<T> count) {
            this.Tag = tag;
            this.ElementType = elementType;
            this.ElementCount = count;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .ArraysVisitor
                .VisitArrayRangeLiteral(this, visitor, context);
        }
    }
}