using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt18.Types;

namespace Attempt18.TypeChecking {
    public class AccessibleTypesVisitor : ITypeVisitor<ImmutableHashSet<LanguageType>> {
        private readonly ITypeCheckScope scope;

        public AccessibleTypesVisitor(ITypeCheckScope scope) {
            this.scope = scope;
        }

        public ImmutableHashSet<LanguageType> VisitArrayType(ArrayType type) {
            return type.ElementType.Accept(this).Add(type);
        }

        public ImmutableHashSet<LanguageType> VisitBoolType(BoolType type) {
            return new LanguageType[] { type }.ToImmutableHashSet();
        }

        public ImmutableHashSet<LanguageType> VisitIntType(IntType type) {
            return new LanguageType[] { type }.ToImmutableHashSet();
        }

        public ImmutableHashSet<LanguageType> VisitNamedType(NamedType type) {
            if (this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                return info.Accept(new IdentifierTargetVisitor<ImmutableHashSet<LanguageType>>() {
                    HandleFunction = _ => new LanguageType[] { type }.ToImmutableHashSet(),
                    HandleComposite = compositeInfo => {
                        return compositeInfo
                            .Signature
                            .Members
                            .Select(x => x.Type)
                            .Select(x => x.Accept(this).Add(x))
                            .Aggregate((x, y) => x.Union(y))
                            .Add(type);
                    }
                });
            }

            throw new Exception("This should never happen");
        }

        public ImmutableHashSet<LanguageType> VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this).Add(type);
        }

        public ImmutableHashSet<LanguageType> VisitVoidType(VoidType type) {
            return new LanguageType[] { type }.ToImmutableHashSet();
        }
    }
}