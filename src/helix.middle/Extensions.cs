using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.TypeChecking;
using Helix.MiddleEnd.Unification;

namespace Helix.MiddleEnd {
    internal static class Extensions {
        public static IHelixType GetSupertype(this IHelixType type) {
            return type.Accept(SupertypeFetcher.Instance);
        }

        public static Option<FunctionType> TryGetFunctionSignature(this IHelixType type, TypeCheckingContext context) {
            if (type is FunctionType f) {
                return f;
            }
            else if (type is NominalType nom) {
                return context.Types.GetSignature(nom.Name).TryGetFunctionSignature(context);
            }
            else {
                return Option.None;
            }
        }

        public static Option<StructType> GetStructSignature(this IHelixType type, TypeCheckingContext context) {
            if (type is StructType structType) {
                return structType;
            }
            else if (type is NominalType nom) {
                return context.Types.GetSignature(nom.Name).GetStructSignature(context);
            }
            else {
                return Option.None;
            }
        }

        public static Option<UnionType> GetUnionSignature(this IHelixType type, TypeCheckingContext context) {
            if (type is UnionType unionType) {
                return unionType;
            }
            else if (type is NominalType nom) {
                return context.Types.GetSignature(nom.Name).GetUnionSignature(context);
            }
            else {
                return Option.None;
            }
        }

        public static Option<ArrayType> GetArraySignature(this IHelixType type, TypeCheckingContext context) {
            if (type is ArrayType arrayType) {
                return arrayType;
            }
            else {
                return Option.None;
            }
        }

        public static Option<PointerType> GetPointerSignature(this IHelixType type, TypeCheckingContext context) {
            if (type is PointerType ptrType) {
                return ptrType;
            }
            else {
                return Option.None;
            }
        }

        public static IEnumerable<IHelixType> GetRecursiveFieldTypes(this IHelixType type, TypeCheckingContext context) {
            return type.Accept(new RecursiveFieldTypesEnumerator(context));
        }

        public static bool HasVoidValue(this IHelixType type, TypeCheckingContext context) {
            return type.Accept(new TypeHasDefaultValueVisitor(context));
        }

        public static bool DoesAliasLValues(this IHelixType type) {
            return type.Accept(TypeDoesAliasLValueVisitor.Instance);
        }

        public static IEnumerable<TypeMemberView> GetMembers(this IHelixType type, TypeCheckingContext context) => GetMembersHelper([], type, context);

        public static IEnumerable<TypeMemberView> GetMembersHelper(IReadOnlyList<string> previous, IHelixType type, TypeCheckingContext context) {
            yield return new TypeMemberView(type, previous);

            if (type.GetStructSignature(context).TryGetValue(out var structType)) {
                foreach (var mem in structType.Members) {
                    var segments = previous.Append(mem.Name).ToArray();

                    foreach (var results in GetMembersHelper(segments, mem.Type, context)) {
                        yield return results;
                    }
                }
            }
        }
    }
}
