using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Immutable;

namespace Attempt17.TypeChecking {
    public class TypeMutatorsVisitor : ITypeVisitor<ImmutableHashSet<LanguageType>> {
        private readonly ITypeCheckScope scope;

        public TypeMutatorsVisitor(ITypeCheckScope scope) {
            this.scope = scope;
        }

        public ImmutableHashSet<LanguageType> VisitArrayType(ArrayType type) {
            return type.ElementType.Accept(this);
        }

        public ImmutableHashSet<LanguageType> VisitBoolType(BoolType type) => ImmutableHashSet<LanguageType>.Empty;

        public ImmutableHashSet<LanguageType> VisitIntType(IntType type) => ImmutableHashSet<LanguageType>.Empty;

        public ImmutableHashSet<LanguageType> VisitNamedType(NamedType type) {
            if (this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                return info.Accept(new IdentifierTargetVisitor<ImmutableHashSet<LanguageType>>() {
                    HandleFunction = _ => ImmutableHashSet<LanguageType>.Empty,
                    HandleComposite = compositeInfo => {
                        return compositeInfo
                            .Signature
                            .Members
                            .Select(x => x.Type)
                            .Aggregate(ImmutableHashSet<LanguageType>.Empty, (x, y) => x.Union(y.Accept(this)));
                    }
                });
            }

            throw new Exception("This should never happen");
        }

        public ImmutableHashSet<LanguageType> VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this).Add(type.InnerType);
        }

        public ImmutableHashSet<LanguageType> VisitVoidType(VoidType type) => ImmutableHashSet<LanguageType>.Empty;
    }
}