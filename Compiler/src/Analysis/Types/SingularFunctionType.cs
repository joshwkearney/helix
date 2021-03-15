using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Trophy.Analysis.Types {
    public class SingularFunctionType : ITrophyType {
        public IdentifierPath FunctionPath { get; }

        public SingularFunctionType(IdentifierPath path) {
            this.FunctionPath = path;
        }


        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public bool HasDefaultValue(ITypeRecorder types) => true;

        public override int GetHashCode() {
            return this.FunctionPath.GetHashCode();
        }

        public override string ToString() {
            return this.FunctionPath.Segments.Last();
        }

        public IOption<SingularFunctionType> AsSingularFunctionType() {
            return Option.Some(this);
        }

        public override bool Equals(object other) {
            return this.Equals(other as ITrophyType);
        }

        public bool Equals(ITrophyType other) {
            return other is SingularFunctionType type && this.FunctionPath == type.FunctionPath;
        }
    }
}