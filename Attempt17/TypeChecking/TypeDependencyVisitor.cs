using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Attempt17.TypeChecking {
    // Returns true or false depending on whether the visited type is dependent on
    // the given type.
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

            if (type.ElementType.Accept(this)) {
                return true;
            }

            if (this.testType.Accept(new TypeDependencyVisitor(type.ElementType, this.scope))) {
                return true;
            }

            return false;
        }

        public bool VisitBoolType(BoolType type) => false;

        public bool VisitIntType(IntType type) => false;

        public bool VisitNamedType(NamedType type) {
            if (this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                return info.Match(
                    varInfo => throw new InvalidOperationException(),
                    funcInfo => false,
                    structInfo => {
                        bool membersDependent = structInfo
                            .Signature
                            .Members
                            .Select(x => x.Type)
                            .Select(x => x.Accept(this))
                            .Any(x => x);

                        if (type == structInfo.StructType) { }

                        return true;
                    });
            }

            throw new Exception("This should never happen");
        }

        public bool VisitVariableType(VariableType type) {
            if (type == this.testType) {
                return true;
            }

            return type.InnerType.Accept(this);
        }

        public bool VisitVoidType(VoidType type) => false;
    }
}