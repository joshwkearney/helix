using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Structs {
    public class StructDeclarationSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public StructInfo StructInfo { get; }

        public StructDeclarationSyntax(T tag, StructInfo info) {
            this.Tag = tag;
            this.StructInfo = info;
        }
    }
}