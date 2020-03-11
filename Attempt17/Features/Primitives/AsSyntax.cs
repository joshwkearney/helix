using System;
using Attempt17.Types;

namespace Attempt17.Features.Primitives {
    public class AsSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public LanguageType TargetType { get; }

        public AsSyntax(T tag, ISyntax<T> target, LanguageType type) {
            this.Tag = tag;
            this.Target = target;
            this.TargetType = type;
        }
    }
}