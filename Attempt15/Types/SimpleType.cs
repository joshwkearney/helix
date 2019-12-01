using JoshuaKearney.Attempt15.Compiling;
using System;

namespace JoshuaKearney.Attempt15.Types {
    public class SimpleType : ITrophyType, IEquatable<SimpleType> {
        public static ITrophyType Int { get; } = new SimpleType(TrophyTypeKind.Int);

        public static ITrophyType Float { get; } = new SimpleType(TrophyTypeKind.Float);

        public static ITrophyType Boolean { get; } = new SimpleType(TrophyTypeKind.Boolean);

        public TrophyTypeKind Kind { get; }

        public bool IsReferenceCounted => false;

        public SimpleType(TrophyTypeKind kind) {
            this.Kind = kind;
        }

        public bool Equals(SimpleType other) {
            return this.Kind == other.Kind;
        }

        public override bool Equals(object obj) {
            if (obj is SimpleType simp) {
                return this.Equals(simp);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Kind.GetHashCode();
        }

        public string GenerateName(CodeGenerateEventArgs args) {
            switch (this.Kind) {
                case TrophyTypeKind.Int:
                    return "int64_t";
                case TrophyTypeKind.Float:
                    return "double";
                case TrophyTypeKind.Boolean:
                    return "char";
                default:
                    throw new Exception();
            }
        }
    }
}