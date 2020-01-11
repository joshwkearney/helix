using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.TypeChecking {
    public class TypeCopiabilityVisitor : ITypeVisitor<TypeCopiability> {
        private readonly IScope scope;

        public TypeCopiabilityVisitor(IScope scope) {
            this.scope = scope;
        }

        public TypeCopiability VisitArrayType(ArrayType type) {
            return TypeCopiability.Conditional;
        }

        public TypeCopiability VisitBoolType(BoolType type) => TypeCopiability.Unconditional;

        public TypeCopiability VisitIntType(IntType type) => TypeCopiability.Unconditional;

        public TypeCopiability VisitNamedType(NamedType type) {
            if (this.scope.FindFunction(type.Path).TryGetValue(out var _)) {
                return TypeCopiability.Unconditional;
            }

            throw new Exception("This should never happen");
        }

        public TypeCopiability VisitVariableType(VariableType type) => TypeCopiability.Conditional;

        public TypeCopiability VisitVoidType(VoidType type) => TypeCopiability.Unconditional;
    }
}
