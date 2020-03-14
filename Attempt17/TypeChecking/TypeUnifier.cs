using System;
using Attempt17.Features;
using Attempt17.Parsing;
using Attempt17.TypeChecking.Unifiers;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public class TypeUnifier : ITypeVisitor<ISyntax<TypeCheckTag>> {
        private readonly TokenLocation location;
        private readonly ISyntax<TypeCheckTag> value;
        private readonly ITypeCheckScope scope;
        private readonly LanguageType targetType;

        public TypeUnifier(ISyntax<TypeCheckTag> value, LanguageType targetType,
            ITypeCheckScope scope, TokenLocation loc) {

            this.value = value;
            this.location = loc;
            this.scope = scope;
            this.targetType = targetType;
        }

        public ISyntax<TypeCheckTag> VisitArrayType(ArrayType type) {
            if (this.targetType == type) {
                return this.value;
            }

            throw TypeCheckingErrors.UnexpectedType(this.location, this.targetType, type);
        }

        public ISyntax<TypeCheckTag> VisitBoolType(BoolType type) {
            if (this.targetType == type) {
                return this.value;
            }

            throw TypeCheckingErrors.UnexpectedType(this.location, this.targetType, type);
        }

        public ISyntax<TypeCheckTag> VisitIntType(IntType type) {
            if (this.targetType == type) {
                return this.value;
            }

            throw TypeCheckingErrors.UnexpectedType(this.location, this.targetType, type);
        }

        public ISyntax<TypeCheckTag> VisitNamedType(NamedType type) {
            if (this.targetType == type) {
                return this.value;
            }

            throw TypeCheckingErrors.UnexpectedType(this.location, this.targetType, type);
        }

        public ISyntax<TypeCheckTag> VisitVariableType(VariableType type) {
            if (this.targetType == type) {
                return this.value;
            }

            throw TypeCheckingErrors.UnexpectedType(this.location, this.targetType, type);
        }

        public ISyntax<TypeCheckTag> VisitVoidType(VoidType type) {
            if (this.targetType == type) {
                return this.value;
            }

            var visitor = new FromVoidUnifier(this.scope);

            if (this.targetType.Accept(visitor).TryGetValue(out var syntax)) {
                return syntax;
            }

            throw TypeCheckingErrors.UnexpectedType(this.location, this.targetType, type);
        }
    }
}
