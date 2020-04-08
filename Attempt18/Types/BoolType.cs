using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt18.Types {
    public class BoolType : LanguageType {
        public static LanguageType Instance { get; } = new BoolType();

        public override LanguageTypeKind Kind => LanguageTypeKind.Bool;

        private BoolType() { }

        public override bool Equals(object other) => other is BoolType;

        public override int GetHashCode() => 11;

        public override string ToString() => "bool";

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitBoolType(this);
        }
    }
}