using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public record PointerType : HelixType {
        public HelixType InnerType { get; }

        public PointerType(HelixType innerType) {
            this.InnerType = innerType;
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ReferenceType;
        }

        public override UnificationKind TestUnification(HelixType other, TypeFrame types) {
            if (other is PointerType pointer) {
                // If we have the same inner type both read-only and read-write
                // pointers are punnable
                if (this.InnerType == pointer.InnerType) {
                    return UnificationKind.Pun;
                }

                var isInnerCompatible = this.InnerType.TestUnification(pointer.InnerType, types) == UnificationKind.Pun;

                // Otherwise, read-only pointers can be punnable if the inner types
                // are punnable
                if (isInnerCompatible) {
                    return UnificationKind.Pun;
                }
            }

            return UnificationKind.None;
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax,
                                            UnificationKind unify, TypeFrame types) {
            if (this.TestUnification(other, types).IsSubsetOf(unify)) {
                // Pointer types only pun, so this just returning the original
                // syntax will be ok
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public override string ToString() {
            return this.InnerType + "*";
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
            yield return this;
            yield return this.InnerType;
        }
    }
}