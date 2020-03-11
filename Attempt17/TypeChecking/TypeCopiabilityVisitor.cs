using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Attempt17.TypeChecking {
    public class TypeCopiabilityVisitor : ITypeVisitor<TypeCopiability> {
        private readonly IScope scope;

        public TypeCopiabilityVisitor(IScope scope) {
            this.scope = scope;
        }

        public TypeCopiability VisitArrayType(ArrayType type) {
            return TypeCopiability.Conditional;
        }

        public TypeCopiability VisitBoolType(BoolType type) => TypeCopiability.Unconditional;

        public TypeCopiability VisitIntType(IntType type) => TypeCopiability.Unconditional;

        public TypeCopiability VisitNamedType(NamedType type) {
            if (this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                return info.Match(
                    varInfo => throw new InvalidOperationException(),
                    funcInfo => TypeCopiability.Unconditional,
                    compositeInfo => {
                        if (compositeInfo.Kind == CompositeKind.Class) {
                            return TypeCopiability.Conditional;
                        }
                        else {
                            var memCopiability = compositeInfo
                                .Signature
                                .Members
                                .Select(x => x.Type)
                                .Select(x => x.Accept(this))
                                .ToArray();

                            if (memCopiability.Any(x => x == TypeCopiability.None)) {
                                return TypeCopiability.None;
                            }
                            else if (memCopiability.All(x => x == TypeCopiability.Unconditional)) {
                                return TypeCopiability.Unconditional;
                            }
                            else {
                                return TypeCopiability.Conditional;
                            }
                        }
                    });
            }

            throw new Exception("This should never happen");
        }

        public TypeCopiability VisitVariableType(VariableType type) => TypeCopiability.Conditional;

        public TypeCopiability VisitVoidType(VoidType type) => TypeCopiability.Unconditional;
    }
}
