using Trophy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class FunctionType : ITrophyType {
        public ITrophyType ReturnType { get; }

        public IReadOnlyList<ITrophyType> ParameterTypes { get; }

        public FunctionType(ITrophyType returnType, IReadOnlyList<ITrophyType> parTypes) {
            this.ReturnType = returnType;
            this.ParameterTypes = parTypes;
        }

        public IOption<FunctionType> AsFunctionType() {
            return Option.Some(this);
        }

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override int GetHashCode() {
            return this.ReturnType.GetHashCode()
                + 3 * this.ParameterTypes.Select(x => x.GetHashCode()).Aggregate(11, (x, y) => x + 59 * y);
        }

        public bool HasDefaultValue(ITypeRecorder types) {
            return false;
        }

        public override string ToString() {
            return "func[" + string.Join(", ", this.ParameterTypes) + "->" + this.ReturnType + "]";
        }

        public override bool Equals(object other) {
            return this.Equals(other as ITrophyType);
        }

        public bool Equals(ITrophyType other) {
            return other is FunctionType func
                && this.ReturnType.Equals(func.ReturnType)
                && this.ParameterTypes.SequenceEqual(func.ParameterTypes);
        }
    }
}