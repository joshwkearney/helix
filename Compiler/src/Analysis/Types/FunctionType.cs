using Trophy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Analysis.Types {
    public class FunctionType : TrophyType {
        public TrophyType ReturnType { get; }

        public IReadOnlyList<TrophyType> ParameterTypes { get; }

        public FunctionType(TrophyType returnType, IReadOnlyList<TrophyType> parTypes) {
            this.ReturnType = returnType;
            this.ParameterTypes = parTypes;
        }

        public override IOption<FunctionType> AsFunctionType() {
            return Option.Some(this);
        }

        public override bool Equals(object other) {
            return other is FunctionType func
                && this.ReturnType == func.ReturnType
                && this.ParameterTypes.SequenceEqual(func.ParameterTypes);
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override int GetHashCode() {
            return this.ReturnType.GetHashCode()
                + 3 * this.ParameterTypes.Select(x => x.GetHashCode()).Aggregate(11, (x, y) => x + 59 * y);
        }

        public override bool HasDefaultValue(ITypeRecorder types) {
            return false;
        }

        public override string ToString() {
            return "func[" + string.Join(", ", this.ParameterTypes) + "->" + this.ReturnType + "]";
        }
    }
}
