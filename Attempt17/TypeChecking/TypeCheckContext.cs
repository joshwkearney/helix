using System;
namespace Attempt17.TypeChecking {
    public class TypeCheckContext {
        public ITypeCheckScope Scope { get; }

        public ITypeChecker Checker { get; }

        public TypeCheckContext(ITypeChecker checker, ITypeCheckScope scope) {
            this.Checker = checker;
            this.Scope = scope;
        }

        public TypeCheckContext WithScope(ITypeCheckScope scope) {
            return new TypeCheckContext(this.Checker, scope);
        }
    }
}