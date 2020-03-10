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
            var opt = this.scope.FindTypeInfo(type.Path);

            if (!opt.Any() || opt.GetValue().AsVariableInfo().Any()) {
                return false;
            }

            return true;
        }

        public bool VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this);
        }

        public bool VisitVoidType(VoidType type) => true;
    }
}