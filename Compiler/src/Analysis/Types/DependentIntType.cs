using System.Collections.Generic;
using Attempt20.Experimental;
using System.Linq;

namespace Attempt20.Analysis.Types {
    public class DependentIntType : TrophyType {
        public override bool IsIntType => true;

        public IReadOnlyList<IBoolAtom> Constraints { get; }

        public DependentIntType(IEnumerable<IBoolAtom> constraints) { 
            this.Constraints = constraints.ToArray();
        }

        public override bool Equals(object other) => other is IntType;

        public override int GetHashCode() => 7;

        public override string ToString() => "int";

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }
    }
}