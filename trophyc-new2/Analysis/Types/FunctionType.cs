using Trophy.Features.Functions;

namespace Trophy.Analysis.Types {
    public record FunctionType : TrophyType {
        public FunctionSignature Signature { get; }

        public FunctionType(FunctionSignature sig) {
            this.Signature = sig;
        }

        public override string ToString() {
            return "func["
                 + this.Signature.ReturnType
                 + "," + String.Join(", ", this.Signature.Parameters.Select(x => x.Type))
                 + "]";
        }
    }
}