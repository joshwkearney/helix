using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositeDeclarationSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public CompositeInfo CompositeInfo { get; }

        public CompositeDeclarationSyntax(T tag, CompositeInfo info) {
            this.Tag = tag;
            this.CompositeInfo = info;
        }
    }
}