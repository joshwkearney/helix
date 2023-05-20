using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using System.Security.AccessControl;
using Helix.Parsing;
using Helix.Collections;

namespace Helix.Analysis.Flow {
    public class FlowFrame : ITypedFrame {
        // General things
        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, LifetimeBundle> SyntaxLifetimes { get; }

        public LifetimeGraph LifetimeGraph { get; }

        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        // Frame-specific things
        public IDictionary<VariablePath, LifetimeBounds> LocalLifetimes { get; }

        public ISet<Lifetime> LifetimeRoots { get; }

        public FlowFrame(TypeFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.Variables = frame.Variables;
            this.Functions = frame.Functions;
            this.Structs = frame.Structs;

            this.LifetimeGraph = new();
            this.SyntaxLifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            this.LocalLifetimes = new Dictionary<VariablePath, LifetimeBounds>();
            this.LifetimeRoots = new HashSet<Lifetime>();
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;

            this.LifetimeGraph = prev.LifetimeGraph;
            this.SyntaxLifetimes = prev.SyntaxLifetimes;

            this.LocalLifetimes = prev.LocalLifetimes;
            this.LifetimeRoots = prev.LifetimeRoots.ToStackedSet();
        }

        public IEnumerable<Lifetime> ReduceRootSet(IEnumerable<Lifetime> roots) {
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

        public IEnumerable<Lifetime> GetRoots(Lifetime lifetime) {
            var roots = this
                .LifetimeGraph
                .GetOutlivedLifetimes(lifetime)
                .Where(x => x.Role != LifetimeRole.Alias)
                .Where(x => x != lifetime);

            roots = this.ReduceRootSet(roots);

            return roots;
        }

        public bool AliasMutationPossible(VariablePath varPath) {
            // Read-only variables can't be mutated
            if (varPath.Member.IsEmpty && !this.Variables[varPath.Variable].IsWritable) {
                return false;
            }

            // TODO: Do the same check for read-only struct fields

            var locationLifetime = this.LocalLifetimes[varPath].LValue;
            var descendents = this
                .LifetimeGraph
                .GetOutlivedLifetimes(locationLifetime)
                .Where(x => x != locationLifetime)
                .ToValueSet();

            // If this variable was never aliased by an addressof operator, it could not have
            // been mutated behind our backs
            if (!descendents.Any()) {
                return false;
            }

            // TODO: If all of our roots were not modified either, then we're still fine
            // If all of our roots' values do not escape to a context where they could
            // be mutated, we're fine

            return true;
        }
    }
}