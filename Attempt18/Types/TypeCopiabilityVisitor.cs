using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt19.Types {
    public class TypeCopiabilityVisitor : ITypeVisitor<Copiability> {
        private readonly HashSet<LanguageType> visited = new HashSet<LanguageType>();
        private readonly TypeChache  types;

        public TypeCopiabilityVisitor(TypeChache  types) {
            this.types = types;
        }

        public Copiability VisitArrayType(ArrayType type) {
            return Copiability.Conditional;
        }

        public Copiability VisitBoolType(BoolType type) {
            return Copiability.Unconditional;
        }

        public Copiability VisitFunctionType(FunctionType type) {
            return Copiability.Unconditional;
        }

        public Copiability VisitIntType(IntType type) {
            return Copiability.Unconditional;
        }

        public Copiability VisitStructType(StructType type) {
            if (this.visited.Contains(type)) {
                return Copiability.Conditional;
            }

            this.visited.Add(type);

            var memResults = this.types
                .Structs[type.Path]
                .Members
                .Select(x => x.Type.Accept(this))
                .ToArray();

            if (memResults.All(x => x == Copiability.Unconditional)) {
                return Copiability.Unconditional;
            }

            if (memResults.Any(x => x == Copiability.None)) {
                return Copiability.None;
            }

            return Copiability.Conditional;
        }

        public Copiability VisitUnresolvedType(UnresolvedType type) {
            throw new InvalidOperationException();
        }

        public Copiability VisitVariableType(VariableType type) {
            return Copiability.Conditional;
        }

        public Copiability VisitVoidType(VoidType type) {
            return Copiability.Unconditional;
        }
    }
}
