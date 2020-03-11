using System;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public interface IScope {
        IOption<TypeInfo> FindTypeInfo(IdentifierPath path);
    }
}
