using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Arrays {
    public class MemberAccessSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public string MemberName { get; }

        public MemberAccessSyntax(T tag, ISyntax<T> target, string memberName) {
            this.Tag = tag;
            this.Target = target;
            this.MemberName = memberName;
        }
    }
}