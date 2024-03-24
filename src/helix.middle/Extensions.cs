using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.TypeVisitors;

namespace Helix.MiddleEnd {
    internal static class Extensions {
        public static Option<FunctionSignature> TryGetFunctionSignature(this IHelixType type, AnalysisContext context) {
            if (type is NominalType nom) {
                return context.Signatures.FunctionSignatures[nom];
            }
            else {
                return Option.None;
            }
        }

        public static Option<StructSignature> GetStructSignature(this IHelixType type, AnalysisContext context) {
            if (type is NominalType nom) {
                return context.Signatures.StructSignatures.GetValueOrNone(nom);
            }
            else if (type is SingularStructType sing) {
                return context.Signatures.StructSignatures.GetValueOrNone(sing.StructType);
            }
            else {
                return Option.None;
            }
        }

        public static Option<UnionSignature> GetUnionSignature(this IHelixType type, AnalysisContext context) {
            if (type is NominalType nom) {
                return context.Signatures.UnionSignatures.GetValueOrNone(nom);
            }
            else if (type is SingularUnionType sing) {
                return context.Signatures.UnionSignatures.GetValueOrNone(sing.UnionType);
            }
            else {
                return Option.None;
            }
        }

        public static Option<ArrayType> GetArraySignature(this IHelixType type, AnalysisContext context) {
            if (type is ArrayType arrayType) {
                return arrayType;
            }
            else {
                return Option.None;
            }
        }

        public static Option<PointerType> GetPointerSignature(this IHelixType type, AnalysisContext context) {
            if (type is PointerType ptrType) {
                return ptrType;
            }
            else {
                return Option.None;
            }
        }

        public static IEnumerable<IHelixType> GetRecursiveFieldTypes(this IHelixType type, AnalysisContext context) {
            return type.Accept(new RecursiveFieldEnumerator(context));
        }

        public static bool HasVoidValue(this IHelixType type, AnalysisContext context) {
            return type.Accept(new HasDefaultValueVisitor(context));
        }

        public static bool DoesAliasLValues(this IHelixType type) {
            return type.Accept(DoesAliasLValueVisitor.Instance);
        }

        public static IEnumerable<MemberView> GetMembers(this IHelixType type, AnalysisContext context) => GetMembersHelper([], type, context);

        public static IEnumerable<MemberView> GetMembersHelper(IReadOnlyList<string> previous, IHelixType type, AnalysisContext context) {
            yield return new MemberView(type, previous);

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
