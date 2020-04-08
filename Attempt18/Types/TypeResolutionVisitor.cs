using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt18.Types {
    public class TypeResolutionVisitor : ITypeVisitor<LanguageType> {
        private readonly HashSet<LanguageType> visited = new HashSet<LanguageType>();
        private readonly NameCache<NameTarget> names;

        public TypeResolutionVisitor(NameCache<NameTarget> names) {
            this.names = names;
        }

        public LanguageType VisitArrayType(ArrayType type) {
            return new ArrayType(type.ElementType.Accept(this));
        }

        public LanguageType VisitBoolType(BoolType type) {
            return type;
        }

        public LanguageType VisitFunctionType(FunctionType type) {
            return type;
        }

        public LanguageType VisitIntType(IntType type) {
            return type;
        }

        public LanguageType VisitStructType(StructType type) {
            return type;
        }

        public LanguageType VisitUnresolvedType(UnresolvedType type) {
            if(names.GetName(type.Path, out var target)) {
                if (target == NameTarget.Function) {
                    return new FunctionType(type.Path);
                }
                else if (target == NameTarget.Struct) {
                    return new StructType(type.Path);
                }
            }

            throw new Exception();
        }

        public LanguageType VisitVariableType(VariableType type) {
            return new VariableType(type.InnerType.Accept(this));
        }

        public LanguageType VisitVoidType(VoidType type) {
            return type;
        }
    }
}
