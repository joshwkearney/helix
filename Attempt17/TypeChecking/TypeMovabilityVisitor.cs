using System;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public class TypeMovabilityVisitor : ITypeVisitor<bool> {
        private readonly IScope scope;

        public TypeMovabilityVisitor(IScope scope) {
            this.scope = scope;
        }

        public bool VisitArrayType(ArrayType type) {
            return true;
        }

        public bool VisitBoolType(BoolType type) {
            return false;
        }

        public bool VisitIntType(IntType type) {
            return false;
        }

        public bool VisitNamedType(NamedType type) {
            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This is not supposed to happen");
            }

            return info.Accept(new IdentifierTargetVisitor<bool>() {
                HandleFunction = _ => false,
                HandleComposite = compositeInfo => compositeInfo.Kind == CompositeKind.Class
            });
        }

        public bool VisitVariableType(VariableType type) {
            return false;
        }

        public bool VisitVoidType(VoidType type) {
            return false;
        }
    }
}