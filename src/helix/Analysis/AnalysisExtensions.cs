using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;

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

        public static IReadOnlyList<VariableCapture> GetCapturedVariables(this ISyntaxTree syntax, ITypedFrame types) {
            return types.CapturedVariables[syntax];
        }

        public static void SetCapturedVariables(this ISyntaxTree syntax, ITypedFrame types) {
            types.CapturedVariables[syntax] = Array.Empty<VariableCapture>();
        }

        public static void SetCapturedVariables(this ISyntaxTree syntax, ISyntaxTree child, ITypedFrame types) {
            types.CapturedVariables[syntax] = child.GetCapturedVariables(types);
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            ISyntaxTree child1, 
            ISyntaxTree child2, 
            ITypedFrame types) {

            var caps = child1.GetCapturedVariables(types)
                .Concat(child2.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            ISyntaxTree child1,
            ISyntaxTree child2,
            ISyntaxTree child3,
            ITypedFrame types) {

            var caps = child1.GetCapturedVariables(types)
                .Concat(child2.GetCapturedVariables(types))
                .Concat(child3.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IEnumerable<ISyntaxTree> children,
            ITypedFrame types) {

            var caps = children
                .SelectMany(x => x.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IdentifierPath variable,
            VariableCaptureKind kind,
            PointerType sig,
            ITypedFrame types) {

            types.CapturedVariables[syntax] = new[] { new VariableCapture(variable, kind, sig) };
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IEnumerable<VariableCapture> caps,
            ITypedFrame types) {

            types.CapturedVariables[syntax] = caps.ToArray();
        }

        public static Bundle<HelixType> GetMembers(this HelixType type, ITypedFrame types) {
            var dict = new Dictionary<IdentifierPath, HelixType>();

            foreach (var (memPath, memType) in GetMemberPaths(type, types)) {
                dict[memPath] = memType;
            }

            return new Bundle<HelixType>(dict);
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
