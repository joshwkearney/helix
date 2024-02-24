using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;
using Helix.Analysis.Predicates;
using Helix.Features.FlowControl;

namespace Helix.Analysis {
    public static class AnalysisExtensions {
        public static Option<PointerType> AsVariable(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) is PointerType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<FunctionType> AsFunction(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) is FunctionType funcSig) {
                return funcSig;
            }
            else {
                return Option.None;
            }            
        }

        public static Option<StructType> AsStruct(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) is StructType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<UnionType> AsUnion(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) is UnionType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<ArrayType> AsArray(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) is ArrayType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static bool IsBool(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) == PrimitiveType.Bool) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsInt(this HelixType type, TypeFrame types) {
            if (type.GetSignature(types) == PrimitiveType.Word) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool TryGetVariable(this TypeFrame types, string name, out PointerType type) {
            return types.Locals
                .GetValueOrNone(name)
                .SelectMany(x => x.Type.AsVariable(types))
                .TryGetValue(out type);
        }

        public static IEnumerable<KeyValuePair<IdentifierPath, HelixType>> GetMembers(this HelixType type, TypeFrame types) {
            var dict = new Dictionary<IdentifierPath, HelixType>();

            foreach (var (memPath, memType) in GetMemberPaths(type, types)) {
                dict[memPath] = memType;
            }

            return dict;
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(
            HelixType type,
            TypeFrame types) {

            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath,
            HelixType type,
            TypeFrame types) {

            yield return (basePath, type);

            if (!type.AsStruct(types).TryGetValue(out var structSig)) {
                yield break;
            }

            foreach (var mem in structSig.Members) {
                var path = basePath.Append(mem.Name);

                foreach (var subs in GetMemberPathsHelper(path, mem.Type, types)) {
                    yield return subs;
                }
            }
        }
    }
}
