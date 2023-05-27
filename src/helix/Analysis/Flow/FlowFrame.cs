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
using Helix.Features.Types;

namespace Helix.Analysis.Flow {
    public class FlowFrame : ITypeContext {
        // Global ITypeFrame things
        public IReadOnlyDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IReadOnlyDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }

        public IReadOnlyDictionary<IdentifierPath, ISyntaxTree> GlobalSyntaxValues { get; }

        public IReadOnlyDictionary<IdentifierPath, HelixType> GlobalNominalSignatures { get; }

        // Global lifetime things
        public IDictionary<ISyntaxTree, LifetimeBounds> SyntaxLifetimes { get; }

        public DataFlowGraph DataFlowGraph { get; }

        // Local lifetime things
        public ImmutableDictionary<IdentifierPath, LifetimeBounds> LocalLifetimes { get; set; }

        public ImmutableHashSet<Lifetime> LifetimeRoots { get; set; }

        public FlowFrame(TypeFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.CapturedVariables = frame.CapturedVariables;

            this.DataFlowGraph = new();
            this.SyntaxLifetimes = new Dictionary<ISyntaxTree, LifetimeBounds>();

            this.LocalLifetimes = ImmutableDictionary<IdentifierPath, LifetimeBounds>.Empty;
            this.LifetimeRoots = ImmutableHashSet<Lifetime>.Empty;
            this.GlobalNominalSignatures = frame.NominalSignatures;
            this.GlobalSyntaxValues = frame.SyntaxValues;
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;

            this.DataFlowGraph = prev.DataFlowGraph;
            this.SyntaxLifetimes = prev.SyntaxLifetimes;

            this.LocalLifetimes = prev.LocalLifetimes;
            this.LifetimeRoots = prev.LifetimeRoots;
            this.GlobalNominalSignatures = prev.GlobalNominalSignatures;
            this.GlobalSyntaxValues = prev.GlobalSyntaxValues;
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
                    if (this.DataFlowGraph.GetEquivalentLifetimes(root).Contains(otherRoot)) {
                        continue;
                    }

                    // If the other root is outlived by this root (and they're not equivalent),
                    // then remove it because "root" is a more useful, longer-lived root
                    if (this.DataFlowGraph.DoesOutlive(root, otherRoot)) {
                        result.Remove(otherRoot);
                    }
                }
            }

            return result;
        }

        public IEnumerable<Lifetime> GetMaximumRoots(Lifetime lifetime) {
            var roots = this
                .DataFlowGraph
                .GetOutlivedLifetimes(lifetime)
                .Where(x => x.Role != LifetimeRole.Alias)
                .Where(x => x != lifetime);

            roots = this.MaximizeRootSet(roots);

            return roots;
        }
    }
}