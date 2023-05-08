using Helix.Features.Arrays;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record ArrayType : HelixType {
        public HelixType InnerType { get; }

        public bool IsWritable { get; }

        public ArrayType(HelixType innerType, bool isWritable) {
            this.InnerType = innerType;
            this.IsWritable = isWritable;
        }

        public override bool CanUnifyTo(HelixType other, EvalFrame types, bool isCast) {
            if (other is PointerType pointer && pointer.IsWritable) {
                if (this.InnerType.Equals(pointer.InnerType)) {
                    return true;
                }
            }

            return base.CanUnifyTo(other, types, isCast);
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, EvalFrame types) {
            if (other is PointerType) {
                return new ArrayToPointerAdapter(this, syntax);
            }

            return base.UnifyTo(other, syntax, isCast, types);
        }

        public override string ToString() {
            return this.InnerType + "[]";
        }

        public override bool IsValueType(ITypedFrame types) => false;

        public override IEnumerable<HelixType> GetContainedTypes(EvalFrame frame) {
            yield return this;
            yield return this.InnerType;
        }
    }
}