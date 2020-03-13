using System;
using Attempt17.Types;

namespace Attempt17 {
    public enum CompositeKind {
        Struct, Class, Union
    }

    public class CompositeInfo : IIdentifierTarget {
        public CompositeSignature Signature { get; }

        public IdentifierPath Path { get; }

        public LanguageType Type => new NamedType(this.Path);

        public CompositeKind Kind { get; }

        public CompositeInfo(CompositeSignature sig, IdentifierPath path, CompositeKind kind) {
            this.Signature = sig;
            this.Path = path;
            this.Kind = kind;
        }

        public T Accept<T>(IIdentifierTargetVisitor<T> visitor) {
            return visitor.VisitComposite(this);
        }
    }
}