using System;
using Attempt17.Types;

namespace Attempt17 {
    public interface IScope {
        IOption<IIdentifierTarget> FindTypeInfo(IdentifierPath path);
    }
}
