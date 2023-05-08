using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record PointerType : HelixType {
        public HelixType InnerType { get; }

        public bool IsWritable { get; }

        public PointerType(HelixType innerType, bool isWritable) {
            this.InnerType = innerType;
            this.IsWritable = isWritable;
        }

        public override bool CanUnifyTo(HelixType other, EvalFrame types, bool isCast) {
            if (this == other) {
                return true;
            }

            if (other is PointerType pointer && !pointer.IsWritable) {
                return true;
            }

            return false;
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, EvalFrame types) => syntax;

        public override string ToString() {
            return this.InnerType + (this.IsWritable ? "*" : "^");
        }

        public override bool IsValueType(ITypedFrame types) => false;

        public override IEnumerable<HelixType> GetContainedTypes(EvalFrame frame) {
            yield return this;
            yield return this.InnerType;
        }
    }
}