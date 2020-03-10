using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Attempt17.TypeChecking {
    public class TypeDefaultValueVisitor : ITypeVisitor<bool> {
        private readonly IScope scope;

        public TypeDefaultValueVisitor(IScope scope) {
            this.scope = scope;
        }

        public bool VisitArrayType(ArrayType type) => true;

        public bool VisitBoolType(BoolType type) => true;

        public bool VisitIntType(IntType type) => true;

        public bool VisitNamedType(NamedType type) {
            if (this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                return info.Match(
                    varInfo => throw new InvalidOperationException(),
                    funcInfo => true,
                    structInfo => structInfo
                        .Signature
                        .Members
                        .Select(x => x.Type)
                        .Select(x => x.Accept(this))
                        .All(x => x));
            }

            throw new Exception("This should never happen");
        }

        public bool VisitVariableType(VariableType type) => false;

        public bool VisitVoidType(VoidType type) => true;
    }
}