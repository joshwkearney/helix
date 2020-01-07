using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Types {
    public interface ITypeVisitor<T> {
        T VisitIntType(IntType type);

        T VisitVoidType(VoidType type);

        T VisitVariableType(VariableType type);

        T VisitNamedType(NamedType type);
    }
}