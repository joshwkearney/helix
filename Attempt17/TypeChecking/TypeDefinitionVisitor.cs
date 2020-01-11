using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.TypeChecking {
    public class TypeDefinitionVisitor : ITypeVisitor<bool> {
        private readonly IScope scope;

        public TypeDefinitionVisitor(IScope scope) {
            this.scope = scope;
        }

        public bool VisitArrayType(ArrayType type) {
            return type.ElementType.Accept(this);
        }

        public bool VisitBoolType(BoolType type) => true;

        public bool VisitIntType(IntType type) => true;

        public bool VisitNamedType(NamedType type) {
            return this.scope.FindFunction(type.Path).Any();
        }

        public bool VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this);
        }

        public bool VisitVoidType(VoidType type) => true;
    }
}