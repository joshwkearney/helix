using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Structs {
    public class StructDeclarationSyntaxTree : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public StructInfo Info { get; }

        public StructDeclarationSyntaxTree(TypeCheckTag tag, StructInfo info) {
            this.Tag = tag;
            this.Info = info;
        }
    }
}