using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt18.Types {
    public class TypeMutatorsVisitor : ITypeVisitor<ImmutableHashSet<LanguageType>> {
        private readonly HashSet<LanguageType> visited = new HashSet<LanguageType>();
        private readonly TypeChache  types;

        public TypeMutatorsVisitor(TypeChache  types) {
            this.types = types;
        }

        public ImmutableHashSet<LanguageType> VisitArrayType(ArrayType type) {
            return type.ElementType.Accept(this).Add(type.ElementType);
        }

        public ImmutableHashSet<LanguageType> VisitBoolType(BoolType type) {
            return new LanguageType[0].ToImmutableHashSet();
        }

        public ImmutableHashSet<LanguageType> VisitFunctionType(FunctionType type) {
            return new LanguageType[0].ToImmutableHashSet();
        }

        public ImmutableHashSet<LanguageType> VisitIntType(IntType type) {
            return new LanguageType[0].ToImmutableHashSet();
        }

        public ImmutableHashSet<LanguageType> VisitStructType(StructType type) {
            if (this.visited.Contains(type)) {
                return new LanguageType[0].ToImmutableHashSet();
            }

            this.visited.Add(type);

            return this.types.Structs[type.Path]
                .Members
                .SelectMany(x => x.Type.Accept(this))
                .ToImmutableHashSet()
                .Add(type);
        }

        public ImmutableHashSet<LanguageType> VisitUnresolvedType(UnresolvedType type) {
            throw new InvalidOperationException();
        }

        public ImmutableHashSet<LanguageType> VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this).Add(type.InnerType);
        }

        public ImmutableHashSet<LanguageType> VisitVoidType(VoidType type) {
            return new LanguageType[0].ToImmutableHashSet();
        }
    }
}
