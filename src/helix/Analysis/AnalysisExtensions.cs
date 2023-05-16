using Helix.Syntax;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public static class AnalysisExtensions {
        public static bool IsTypeChecked(this ISyntaxTree syntax, ITypedFrame types) {
            return types.ReturnTypes.ContainsKey(syntax);
        }

        public static HelixType GetReturnType(this ISyntaxTree syntax, ITypedFrame types) {
            return types.ReturnTypes[syntax];
        }

        public static void SetReturnType(this ISyntaxTree syntax, HelixType type, ITypedFrame types) {
            types.ReturnTypes[syntax] = type;
        }

        public static Dictionary<IdentifierPath, HelixType> GetMembers(this HelixType type, ITypedFrame types) {
            var dict = new Dictionary<IdentifierPath, HelixType>();

            foreach (var (memPath, memType) in GetMemberPaths(type, types)) {
                dict[memPath] = memType;
            }

            return dict;
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(
            HelixType type,
            ITypedFrame types) {

            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath,
            HelixType type,
            ITypedFrame types) {

            yield return (basePath, type);

            if (type is not NamedType named) {
                yield break;
            }

            if (!types.Structs.TryGetValue(named.Path, out var structSig)) {
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
