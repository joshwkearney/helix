using Helix.Syntax.ParseTree.Structs;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Primitives;
using Helix.Syntax.TypedTree.Unions;
using Helix.Types;

namespace Helix.TypeChecking;

public static class TypeUnifier {
    private delegate ITypedExpression Unifier(ITypedExpression expression, TypeFrame frame);

    private readonly struct UnificationResult {
        public static UnificationResult None { get; } = new UnificationResult {
            Kind = UnificationKind.None,
            Unifier = null
        };

        public static UnificationResult Identity { get; } = new UnificationResult {
            Kind = UnificationKind.Pun,
            Unifier = (s, t) => s
        };

        public static UnificationResult Pun(HelixType adaptedType) {
            return new UnificationResult {
                Kind = UnificationKind.Pun,
                Unifier = (s, t) => new TypeAdapterExpression {
                    Operand = s,
                    ReturnType = adaptedType,
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
        
    public static bool CanPunTo(this HelixType type1, HelixType type2, TypeFrame types) {
        return TryUnify(type1, type2, types).Kind.IsSubsetOf(UnificationKind.Pun);
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

        var abstract1 = type1.GetSignature(types);
        var abstract2 = type2.GetSignature(types);

        if (abstract1 == abstract2) {
            resultType = abstract1;
            return true;
        }

        resultType = PrimitiveType.Void;
        return false;
    }

    public static bool CanUnifyTo(this ITypedExpression fromParse, HelixType toType, TypeFrame types) {
        return fromParse.ReturnType.CanUnifyTo(toType, types);
    }
        
    public static bool CanPunTo(this ITypedExpression fromParse, HelixType toType, TypeFrame types) {
        return fromParse.ReturnType.CanPunTo(toType, types);
    }

    public static ITypedExpression UnifyTo(this ITypedExpression fromParse, HelixType toType, TypeFrame types) {
        var fromType = fromParse.ReturnType;

        if (!fromType.CanUnifyTo(toType, types)) {
            throw TypeException.UnexpectedType(fromParse.Location, toType, fromType);
        }

        return TryUnify(fromType, toType, types).Unifier(fromParse, types);
    }

    public static ITypedExpression UnifyFrom(this ITypedExpression syntax1, ITypedExpression syntax2, TypeFrame types) {
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
        else if (first is SingularBoolType || first is PredicateBool) {
            return TryUnifyFromSingularBool(second, types);
        }
        else if (first is ReferenceType pointerType) {
            return TryUnifyFromPointer(pointerType, second, types);
        }
        else if (first is ArrayType arrayType) {
            return TryUnifyFromArray(arrayType, second, types);
        }
        else if (first is NominalType nom) {
            return TryUnifyFromNominalType(nom, second, types);
        }
        else if (first is SingularUnionType union) {
            return TryUnifyFromSingularUnion(union, second, types);
        }

        return UnificationResult.None;
    }
        
    private static UnificationResult TryUnifyFromSingularUnion(SingularUnionType type, HelixType second, TypeFrame types) {
        if (type.UnionSignature == second) {
            return UnificationResult.Pun(second);
        }
        else if (type.MemberNames.Count == 1) {
            var memName = type.MemberNames.First();
            var memType = type.UnionSignature.Members.First(x => x.Name == memName).Type;

            if (memType.CanUnifyTo(second, types)) {
                return new UnificationResult {
                    Kind = UnificationKind.Convert,
                    Unifier = (syntax, types) => new TypedUnionMemberAccessExpression {
                        Location = syntax.Location,
                        MemberName = memName,
                        Operand = syntax,
                        ReturnType = memType
                    }
                };
            }
        }

        return UnificationResult.None;
    }

    private static UnificationResult TryUnifyFromNominalType(NominalType type, HelixType second, TypeFrame types) {
        if (types.TryGetVariable(type.Path, out var refinement)) {
            return TryUnify(refinement, second, types);
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

    private static UnificationResult TryUnifyFromPointer(ReferenceType pointer1, HelixType second, TypeFrame types) {
        if (second is ReferenceType pointer2) {
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
        var memsConvertable = sig.Members.All(x => PrimitiveType.Void.CanUnifyTo(x.Type, types));

        if (!memsConvertable) {
            return UnificationResult.None;
        }

        return new UnificationResult {
            Kind = UnificationKind.Convert,
            Unifier = (syntax, t) => {
                var newStruct = new NewStructExpression {
                    Location = syntax.Location,
                    StructSignature = sig,
                    StructType = structType
                };
                    
                var block = new TypedCompoundExpression {
                    First = syntax,
                        
                    // TODO: Don't be lazy and construct a new struct synatx without type checking
                    Second = newStruct.CheckTypes(types).Result,
                };

                return block;
            }
        };
    }

    private static UnificationResult TryUnifyVoidToUnion(HelixType unionType, UnionType sig, TypeFrame types) {
        if (!PrimitiveType.Void.CanUnifyTo(sig.Members[0].Type, types)) {
            return UnificationResult.None;
        }

        return new UnificationResult {
            Kind = UnificationKind.Convert,
            Unifier = (syntax, types) => {
                var type = sig.Members[0].Type;
                var value = new VoidLiteral { Location = syntax.Location }.UnifyTo(type, types);
                    
                var newUnion = new TypedNewUnionExpression {
                    Location = syntax.Location,
                    UnionSignature = sig,
                    UnionType = unionType,
                    Name = sig.Members[0].Name,
                    Value = value,
                };

                var block = new TypedCompoundExpression {
                    First = syntax,
                    Second = newUnion,
                };

                return block;
            }
        };
    }
}