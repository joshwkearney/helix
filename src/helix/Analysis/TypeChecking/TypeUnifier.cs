using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Structs;
using Helix.Features.Unions;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking {
    public static class TypeUnifier {
        private delegate ISyntax Unifier(ISyntax tree, TypeFrame frame);

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
                        var block = BlockParse.FromMany(s.Location, [s]).CheckTypes(t);

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

        public static bool CanUnifyTo(this ISyntax fromParse, HelixType toType, TypeFrame types) {
            return fromParse.ReturnType.CanUnifyTo(toType, types);
        }

        public static ISyntax UnifyTo(this ISyntax fromParse, HelixType toType, TypeFrame types) {
            var fromType = fromParse.ReturnType;

            if (!fromType.CanUnifyTo(toType, types)) {
                throw TypeException.UnexpectedType(fromParse.Location, toType, fromType);
            }

            return TryUnify(fromType, toType, types).Unifier(fromParse, types);
        }

        public static ISyntax UnifyFrom(this ISyntax syntax1, ISyntax syntax2, TypeFrame types) {
            var type1 = syntax1.ReturnType;
            var type2 = syntax2.ReturnType;

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
            else if (first is SingularWordType) {
                return TryUnifyFromSingularInt(second, types);
            }
            else if (first is SingularBoolType) {
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
            if (second == PrimitiveType.Word) {
                return UnificationResult.Pun(second);
            }
            else {
                return TryUnify(PrimitiveType.Word, second, types);
            }
        }

        private static UnificationResult TryUnifyFromBool(HelixType second, TypeFrame types) {
            if (second == PrimitiveType.Word) {
                return UnificationResult.Pun(second);
            }
            else {
                return UnificationResult.None;
            }
        }

        private static UnificationResult TryUnifyFromVoid(HelixType second, TypeFrame types) {
            if (second == PrimitiveType.Word || second == PrimitiveType.Bool) {
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
                    var block = new BlockParse(syntax.Location,
                        syntax,
                        new NewStructParseSyntax {
                            Location = syntax.Location,
                            Signature = sig,
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
                    var block = new BlockParse(
                        syntax.Location,
                        syntax,
                        new NewUnionParseSyntax {
                            Location = syntax.Location,
                            Signature = sig
                        });

                    return block.CheckTypes(t);
                }
            };
        }
    }
}
