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

        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        // Frame-specific things
        public ImmutableDictionary<VariablePath, LifetimeBounds> LocalLifetimes { get; set; }

        public ImmutableHashSet<Lifetime> LifetimeRoots { get; set; }

        public FlowFrame(TypeFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.CapturedVariables = frame.CapturedVariables;

            this.Variables = frame.Variables;
            this.Functions = frame.Functions;
            this.Structs = frame.Structs;

            this.LifetimeGraph = new();
            this.SyntaxLifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            this.LocalLifetimes = ImmutableDictionary<VariablePath, LifetimeBounds>.Empty;
            this.LifetimeRoots = ImmutableHashSet<Lifetime>.Empty;
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;

            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;

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


        public IEnumerable<LifetimeBounds> GetAliasedLocals(Lifetime lifetime, HelixType pointerType) {
            // THE DEAL WITH ALIASING: Pointers can alias in Helix, which means that
            // any pointer could be a copy of another pointer OR an address of a local
            // variable. In Helix it is the responsibility of the pointer dereferencer
            // to get a fresh value and ensure that any aliasing occuring in the
            // background didn't affect the local program. The C compiler is also pretty
            // good at doing this. Anyway, that means the only thing we really have to
            // be concerned about here is when a pointer aliases a local that is still
            // in scope. In this case, we need to find that local and update the mutation
            // count in its lifetime so the lifetime inference algorithm doesn't mix up
            // the old and new values. There are further aliasing concerns around function
            // calls, but this is only for assignment.

            // The strategy here will be to do a reverse traversal of the flow graph and
            // find any local variable locations that must outlive our pointer dereference
            // that also have the correct type. We will also assume that all roots alias 
            // each other, so if the lifetime we are starting from came from a root we
            // have to search from every other root for aliased variables

            var precursors = this.LifetimeGraph.GetPrecursorLifetimes(lifetime)
                .ToValueSet();

            var results = precursors.Where(x => x.Origin == LifetimeOrigin.LocalLocation);

            if (precursors.Any(x => x.Role == LifetimeRole.Root)) {
                var descendents = this.LifetimeRoots
                    .SelectMany(x => this.LifetimeGraph.GetOutlivedLifetimes(lifetime))                    
                    .Where(x => x.Origin == LifetimeOrigin.LocalLocation);

                results = results.Concat(descendents);
            }

            return results
                .Where(x => this.Variables[x.Path.Variable].Type.GetMembers(this).Values.Contains(pointerType))
                .Select(x => this.LocalLifetimes[x.Path])
                .ToArray();
        }
    }
}