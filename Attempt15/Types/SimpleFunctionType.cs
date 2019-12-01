using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Types {
    public class SimpleFunctionType : IFunctionType {
        public ITrophyType ReturnType { get; }

        public IReadOnlyList<ITrophyType> ArgTypes { get; }

        public SimpleFunctionType(ITrophyType returnType, IEnumerable<ITrophyType> args) {
            this.ReturnType = returnType;
            this.ArgTypes = args.ToArray();
        }
    }
}