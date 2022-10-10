using System.Diagnostics.CodeAnalysis;

namespace Trophy.Parsing {
    public class IntType : ITrophyType {
        public bool IsIntType => true;

        public IntType() { }

        public override int GetHashCode() => 7;

        public override string ToString() => "int";

        public bool HasDefaultValue(NameTable types) => true;

        public override bool Equals(object other) => other is IntType;


        public bool Equals(ITrophyType other) => other is IntType;
    }
}