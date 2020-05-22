using Attempt18.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt18.TypeChecking {
    public class TypeDefinitionVisitor : ITypeVisitor<bool> {
        private readonly ITypeCheckScope scope;

        public TypeDefinitionVisitor(ITypeCheckScope scope) {
            this.scope = scope;
        }

        public bool VisitArrayType(ArrayType type) {
            return type.ElementType.Accept(this);
        }

        public bool VisitBoolType(BoolType type) => true;

        public bool VisitIntType(IntType type) => true;

        public bool VisitNamedType(NamedType type) {
            var basePath = this.scope.Path;

            while (true) {
                var path = basePath.Append(type.Path);
                var opt = this.scope.FindTypeInfo(path);

                if (opt.TryGetValue(out var target)) {
                    return target.AsComposite().Any() || target.AsFunction().Any();                        
                }

                if (basePath.Segments.Count == 0) {
                    break;
                }

                basePath = basePath.Pop();
            }            

            return true;
        }

        public bool VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this);
        }

        public bool VisitVoidType(VoidType type) => true;
    }
}