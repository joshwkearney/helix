using System;
using Attempt17.Types;

namespace Attempt17 {
    public class ReservedIdentifier : IIdentifierTarget {
        public IdentifierPath Path { get; }

        public LanguageType Type { get; }

        public ReservedIdentifier(IdentifierPath path, LanguageType type) {
            this.Path = path;
            this.Type = type;
        }

        public T Accept<T>(IIdentifierTargetVisitor<T> visitor) {
            return visitor.VisitReserved(this);
        }
    }
}