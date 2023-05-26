using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using System.Security.AccessControl;
using Helix.Parsing;
using Helix.Collections;
using System.Linq;
using System.Collections.Immutable;

namespace Helix.Analysis.Flow {
    public class FlowFrame : ITypedFrame {
        // General things
        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }

        public IDictionary<ISyntaxTree, LifetimeBundle> SyntaxLifetimes { get; }

        public LifetimeGraph LifetimeGraph { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<IdentifierPath, StructSignature> Unions { get; }

        // Frame-specific things
        public ImmutableDictionary<VariablePath, LifetimeBounds> LocalLifetimes { get; set; }

        public ImmutableHashSet<Lifetime> LifetimeRoots { get; set; }

        public FlowFrame(TypeFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.CapturedVariables = frame.CapturedVariables;

            this.Functions = frame.Functions;
            this.Structs = frame.Structs;
            this.Unions = frame.Unions;

            this.LifetimeGraph = new();
            this.SyntaxLifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            this.LocalLifetimes = ImmutableDictionary<VariablePath, LifetimeBounds>.Empty;
            this.LifetimeRoots = ImmutableHashSet<Lifetime>.Empty;
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;

            this.Functions = prev.Functions;
            this.Structs = prev.Structs;
            this.Unions = prev.Unions;

            this.LifetimeGraph = prev.LifetimeGraph;
            this.SyntaxLifetimes = prev.SyntaxLifetimes;

            this.LocalLifetimes = prev.LocalLifetimes;
            this.LifetimeRoots = prev.LifetimeRoots;
        }

        private IEnumerable<Lifetime> MaximizeRootSet(IEnumerable<Lifetime> roots) {
            var result = new List<Lifetime>(roots);

            foreach (var root in roots) {
                foreach (var otherRoot in roots) {
                    // Don't compare a lifetime against itself
                    if (root == otherRoot) {
                        continue;
                    }

                    // If these two lifetimes are equivalent (ie, they are supposed to
                    // outlive each other), then keep both as roots
                    if (this.LifetimeGraph.GetEquivalentLifetimes(root).Contains(otherRoot)) {
                        continue;
                    }

                    // If the other root is outlived by this root (and they're not equivalent),
                    // then remove it because "root" is a more useful, longer-lived root
                    if (this.LifetimeGraph.DoesOutlive(root, otherRoot)) {
                        result.Remove(otherRoot);
                    }
                }
            }

            return result;
        }

        public IEnumerable<Lifetime> GetMaximumRoots(Lifetime lifetime) {
            var roots = this
                .LifetimeGraph
                .GetOutlivedLifetimes(lifetime)
                .Where(x => x.Role != LifetimeRole.Alias)
                .Where(x => x != lifetime);

            roots = this.MaximizeRootSet(roots);

            return roots;
        }
    }
}