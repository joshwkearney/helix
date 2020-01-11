using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.TypeChecking {
    public class TypeDefaultValueVisitor : ITypeVisitor<bool> {
        public bool VisitArrayType(ArrayType type) => true;

        public bool VisitBoolType(BoolType type) => true;

        public bool VisitIntType(IntType type) => true;

        public bool VisitNamedType(NamedType type) => true;

        public bool VisitVariableType(VariableType type) => false;

        public bool VisitVoidType(VoidType type) => true;
    }
}