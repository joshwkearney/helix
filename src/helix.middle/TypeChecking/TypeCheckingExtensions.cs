using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Unification;

namespace Helix.MiddleEnd.TypeChecking {
    internal static class TypeCheckingExtensions {
        public static IHelixType GetSupertype(this IHelixType type) {
            return type.Accept(SupertypeFetcher.Instance);
        }

        public static Option<FunctionType> GetFunctionSignature(this IHelixType type, TypeCheckingContext context) {
            if (type is FunctionType f) {
                return f;
            }
            else if (type is NominalType nom) {
                Assert.IsTrue(context.Types.ContainsType(nom.Name));

                return context.Types[nom.Name].GetFunctionSignature(context);
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
                Assert.IsTrue(context.Types.ContainsType(nom.Name));

                return context.Types[nom.Name].GetStructSignature(context);
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
                Assert.IsTrue(context.Types.ContainsType(nom.Name));

                return context.Types[nom.Name].GetUnionSignature(context);
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
    }
}
