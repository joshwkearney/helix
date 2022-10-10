using System.Diagnostics.CodeAnalysis;

namespace Trophy.Parsing {
    public class BoolType : ITrophyType {
        public bool IsBoolType => true;

        public BoolType() { }

        public override int GetHashCode() => 11;

        public override string ToString() => "bool";

        public bool HasDefaultValue(NameTable types) => true;

        public override bool Equals([AllowNull] object other) => other is BoolType;

        public bool Equals([AllowNull] ITrophyType other) => other is BoolType;
    }
}