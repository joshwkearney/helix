using System.Linq;

namespace Trophy.Analysis.Types {
    public class SingularFunctionType : TrophyType {
        public IdentifierPath FunctionPath { get; }

        public SingularFunctionType(IdentifierPath path) {
            this.FunctionPath = path;
        }

        public override bool Equals(object other) {
            return other is SingularFunctionType type && this.FunctionPath == type.FunctionPath;
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override int GetHashCode() {
            return this.FunctionPath.GetHashCode();
        }

        public override string ToString() {
            return this.FunctionPath.Segments.Last();
        }

        public override IOption<SingularFunctionType> AsSingularFunctionType() {
            return Option.Some(this);
        }
    }
}