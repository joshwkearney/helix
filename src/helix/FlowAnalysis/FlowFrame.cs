using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Parsing;
using System.Runtime.CompilerServices;

namespace Helix.Analysis {
    public interface ITypedFrame {
        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }
    }

    public class FlowFrame : ITypedFrame {
        // General things
        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, LifetimeBundle> Lifetimes { get; }

        public LifetimeGraph LifetimeGraph { get; }

        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        // Frame-specific things
        public IDictionary<IdentifierPath, Lifetime> VariableLifetimes { get; }

        public FlowFrame(EvalFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.Variables = frame.Variables;
            this.Functions = frame.Functions;
            this.Structs = frame.Structs;

            this.LifetimeGraph = new();
            this.Lifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();
            this.VariableLifetimes = new Dictionary<IdentifierPath, Lifetime>();
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;

            this.LifetimeGraph = prev.LifetimeGraph;
            this.Lifetimes = prev.Lifetimes;
            this.VariableLifetimes = new StackedDictionary<IdentifierPath, Lifetime>(prev.VariableLifetimes);
        }

        public LifetimeBundle GetVariableBundle(IdentifierPath path) {
            var sig = this.Variables[path];
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (memPath, _) in GetMemberPaths(sig.Type, this)) {
                bundleDict[memPath] = this.VariableLifetimes[memPath];
            }

            return new LifetimeBundle(bundleDict);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(HelixType type, ITypedFrame types) {
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