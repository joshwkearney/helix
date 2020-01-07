using Attempt17.CodeGeneration;
using Attempt17.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17.Types {
    public class NamedType : LanguageType {
        public IdentifierPath Path { get; }

        public NamedType(IdentifierPath path) {
            this.Path = path;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is NamedType named) {
                return this.Path == named.Path;
            }

            return false;
        }

        public override int GetHashCode() => this.Path.GetHashCode();

        public override string ToString() => this.Path.Segments.Last();

        public override string GenerateCType() {
            return this.Path.ToCName();
        }

        public override bool IsDefinedWithin(Scope scope) {
            return false;
        }
    }
}