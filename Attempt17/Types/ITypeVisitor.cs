using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Types {
    public interface ITypeVisitor<T> {
        T VisitIntType(IntType type);

        T VisitVoidType(VoidType type);

        T VisitBoolType(BoolType type);

        T VisitVariableType(VariableType type);

        T VisitNamedType(NamedType type);

        T VisitArrayType(ArrayType type);
    }
}