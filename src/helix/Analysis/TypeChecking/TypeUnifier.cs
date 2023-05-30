using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking {
    public static class TypeUnifier {
        private delegate ISyntaxTree Unifier(ISyntaxTree tree, TypeFrame frame);

        private readonly struct UnificationResult {
            public static UnificationResult None { get; } = new UnificationResult() {
                Kind = UnificationKind.None,
                Unifier = null
            };

            public static UnificationResult Identity { get; } = new UnificationResult() {
                Kind = UnificationKind.Pun,
                Unifier = (s, t) => s
            };

            public static UnificationResult Pun(HelixType adaptedType) {
                return new UnificationResult() {
                    Kind = UnificationKind.Pun,
                    Unifier = (s, t) => {
                        var block = new BlockSyntax(s.Location, new[] { s }).CheckTypes(t);
                        block.SetReturnType(adaptedType, t);

                        return block;
                    }
                };
            }

            public UnificationKind Kind { get; init; }

            public Unifier Unifier { get; init; }
        }

        public static bool HasDefaultValue(this HelixType type, TypeFrame types) {
            return PrimitiveType.Void.CanUnifyTo(type, types);
        }

        public static bool CanUnifyTo(this HelixType type1, HelixType type2, TypeFrame types) {
            return TryUnify(type1, type2, types).Kind.IsSubsetOf(UnificationKind.Convert);
        }

        public static bool CanUnifyFrom(this HelixType type1, HelixType type2, TypeFrame types, out HelixType resultType) {
            if (type2.CanUnifyTo(type1, types)) {
                resultType = type1;
                return true;
            }
            else if (type1.CanUnifyTo(type2, types)) {
                resultType = type2;
                return true;
            }

            var abstract1 = type1.GetMutationSupertype(types);
            var abstract2 = type2.GetMutationSupertype(types);

            if (abstract1 == abstract2) {
                resultType = abstract1;
                return true;
            }

            resultType = PrimitiveType.Void;
            return false;
        }

        public static bool CanUnifyTo(this ISyntaxTree fromSyntax, HelixType toType, TypeFrame types) {
            return fromSyntax.GetReturnType(types).CanUnifyTo(toType, types);
        }

        public static ISyntaxTree UnifyTo(this ISyntaxTree fromSyntax, HelixType toType, TypeFrame types) {
            var fromType = fromSyntax.GetReturnType(types);

            if (!fromType.CanUnifyTo(toType, types)) {
                throw TypeException.UnexpectedType(fromSyntax.Location, toType, fromType);
            }

            return TryUnify(fromType, toType, types).Unifier(fromSyntax, types);
        }

        public static ISyntaxTree UnifyFrom(this ISyntaxTree syntax1, ISyntaxTree syntax2, TypeFrame types) {
            var type1 = syntax1.GetReturnType(types);
            var type2 = syntax2.GetReturnType(types);

            if (!type1.CanUnifyFrom(type2, types, out var result)) {
                throw TypeException.UnexpectedType(syntax1.Location, type2, type1);
            }

            return syntax1.UnifyTo(result, types);
        }

        private static UnificationResult TryUnify(HelixType first, HelixType second, TypeFrame types) {
            if (first == second) {
                return UnificationResult.Identity;
            }

            if (first == PrimitiveType.Void) {
                return TryUnifyFromVoid(second, types);
            }
            else if (first == PrimitiveType.Bool) {
                return TryUnifyFromBool(second, types);
            }
            else if (first is SingularIntType) {
                return TryUnifyFromSingularInt(second, types);
            }
            else if (first is SingularBoolType || first is PredicateBool) {
                return TryUnifyFromSingularBool(second, types);
            }
            else if (first is PointerType pointerType) {
                return TryUnifyFromPointer(pointerType, second, types);
            }
            else if (first is ArrayType arrayType) {
                return TryUnifyFromArray(arrayType, second, types);
            }
            else if (first is NominalType nom) {
                return TryUnifyFromNominalType(nom, second, types);
            }

            return UnificationResult.None;
        }

        private static UnificationResult TryUnifyFromNominalType(NominalType type, HelixType second, TypeFrame types) {
            if (type.Kind == NominalTypeKind.Variable) {
                var typeSig = type.AsVariable(types).GetValue();

                if (second is PointerType secondPtr) {
                    if (TryUnify(typeSig.InnerType, secondPtr.InnerType, types).Kind == UnificationKind.Pun) {
                        return UnificationResult.Pun(second);
                    }
                }
            }

            return UnificationResult.None;
        }

        private static UnificationResult TryUnifyFromArray(ArrayType array1, HelixType second, TypeFrame types) {
            if (second is ArrayType array2) {
                var innerCompatibility = TryUnify(array1.InnerType, array2.InnerType, types).Kind;

                if (innerCompatibility == UnificationKind.Pun) {
                    return UnificationResult.Pun(array2);
                }
            }

            return UnificationResult.None;
        }

        private static UnificationResult TryUnifyFromPointer(PointerType pointer1, HelixType second, TypeFrame types) {
            if (second is PointerType pointer2) {
                var innerCompatibility = TryUnify(pointer1.InnerType, pointer2.InnerType, types).Kind;

                if (innerCompatibility == UnificationKind.Pun) {
                    return UnificationResult.Pun(pointer2);
                }
            }

            return UnificationResult.None;
        }

        private static UnificationResult TryUnifyFromSingularBool(HelixType second, TypeFrame types) {
            if (second == PrimitiveType.Bool) {
                return UnificationResult.Pun(second);
            }
            else {
                return TryUnify(PrimitiveType.Bool, second, types);
            }
        }

        private static UnificationResult TryUnifyFromSingularInt(HelixType second, TypeFrame types) {
            if (second == PrimitiveType.Int) {
                return UnificationResult.Pun(second);
            }
            else {
                return TryUnify(PrimitiveType.Int, second, types);
            }
        }

        private static UnificationResult TryUnifyFromBool(HelixType second, TypeFrame types) {
            if (second == PrimitiveType.Int) {
                return UnificationResult.Pun(second);
            }
            else {
                return UnificationResult.None;
            }
        }

        private static UnificationResult TryUnifyFromVoid(HelixType second, TypeFrame types) {
            if (second == PrimitiveType.Int || second == PrimitiveType.Float || second == PrimitiveType.Bool) {
                return UnificationResult.Pun(second);
            }
            else if (second.AsStruct(types).TryGetValue(out var structSig)) {
                return TryUnifyVoidToStruct(second, structSig, types);
            }
            else if (second.AsUnion(types).TryGetValue(out var unionSig)) {
                return TryUnifyVoidToUnion(second, unionSig, types);
            }

            return UnificationResult.None;
        }

        private static UnificationResult TryUnifyVoidToStruct(HelixType structType, StructType sig, TypeFrame types) {
            var memsConvertable = sig.Members
                .All(x => PrimitiveType.Void.CanUnifyTo(x.Type, types));

            if (!memsConvertable) {
                return UnificationResult.None;
            }

            return new UnificationResult() {
                Kind = UnificationKind.Convert,
                Unifier = (syntax, t) => {
                    var block = new BlockSyntax(syntax.Location, new[] {
                        syntax,
                        new NewStructSyntax(
                            syntax.Location, 
                            structType, 
                            sig, 
                            Array.Empty<string>(), 
                            Array.Empty<ISyntaxTree>(), 
                            types.Scope)
                    });

                    return block.CheckTypes(t);
                }
            };
        }

        private static UnificationResult TryUnifyVoidToUnion(HelixType unionType, UnionType sig, TypeFrame types) {
            if (!PrimitiveType.Void.CanUnifyTo(sig.Members[0].Type, types)) {
                return UnificationResult.None;
            }

            return new UnificationResult() {
                Kind = UnificationKind.Convert,
                Unifier = (syntax, t) => {
                    var block = new BlockSyntax(syntax.Location, new[] {
                        syntax,
                        new NewUnionSyntax(
                            syntax.Location,
                            sig,
                            sig,
                            Array.Empty<string>(),
                            Array.Empty<ISyntaxTree>())
                    });

                    return block.CheckTypes(t);
                }
            };
        }
    }
}
