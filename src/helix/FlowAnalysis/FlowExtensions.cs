using System;
using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace helix.FlowAnalysis {
    public static class FlowExtensions {
        public static LifetimeBundle GetLifetimes(this ISyntaxTree syntax, FlowFrame flow) {
            return flow.Lifetimes[syntax];
        }

        public static void SetLifetimes(this ISyntaxTree syntax, LifetimeBundle bundle, FlowFrame flow) {
            flow.Lifetimes[syntax] = bundle;
        }

        public static LifetimeBundle GetVariableBundle(this FlowFrame flow, IdentifierPath path) {
            var sig = flow.Variables[path];
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (relPath, _) in GetMemberPaths(sig.Type, flow)) {
                var memPath = path.Append(relPath);

                if (flow.VariableValueLifetimes.TryGetValue(memPath, out var lifetime)) {
                    bundleDict[relPath] = lifetime;
                }
                else {
                    bundleDict[relPath] = new Lifetime(memPath, 0, LifetimeKind.Passthrough);
                }
            }

            return new LifetimeBundle(bundleDict);
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

