using System.Collections.Generic;

namespace JoshuaKearney.Attempt15.Types {
    public interface IFunctionType {
        ITrophyType ReturnType { get; }

        IReadOnlyList<ITrophyType> ArgTypes { get; }
    }
}