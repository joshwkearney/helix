using System;
using System.Collections.Generic;
using System.Text;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Arrays {
    public class ArraySizeAccessSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public ISyntax<TypeCheckTag> Target { get; }

        public ArraySizeAccessSyntax(TypeCheckTag tag, ISyntax<TypeCheckTag> target) {
            this.Tag = tag;
            this.Target = target;
        }
    }
}