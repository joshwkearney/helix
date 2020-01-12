using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.TypeChecking {
    public class TypeDependencyVisitor : ITypeVisitor<bool> {
        private readonly LanguageType testType;
        private readonly IScope scope;

        public TypeDependencyVisitor(LanguageType test, IScope scope) {
            this.testType = test;
        }

        public bool VisitArrayType(ArrayType type) {
            if (type == this.testType) {
                return true;
            }

            return type.ElementType.Accept(this);
        }

        public bool VisitBoolType(BoolType type) => false;

        public bool VisitIntType(IntType type) => false;

        public bool VisitNamedType(NamedType type) {
            if (this.scope.FindFunction(type.Path).Any()) {
                return false;
            }

            return true;
        }

        public bool VisitVariableType(VariableType type) {
            if (type == this.testType) {
                return true;
            }

            return this.testType.Accept(this);
        }

        public bool VisitVoidType(VoidType type) => false;
    }
}