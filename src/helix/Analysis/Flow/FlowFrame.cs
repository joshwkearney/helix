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

        public IDictionary<ISyntaxTree, LifetimeBundle> Lifetimes { get; }

        public LifetimeGraph LifetimeGraph { get; }

        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        // Frame-specific things
        public IDictionary<VariablePath, Lifetime> LocationLifetimes { get; }

        public IDictionary<VariablePath, Lifetime> StoredValueLifetimes { get; }

        public FlowFrame(TypeFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.Variables = frame.Variables;
            this.Functions = frame.Functions;
            this.Structs = frame.Structs;

            this.LifetimeGraph = new();
            this.Lifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            this.LocationLifetimes = new Dictionary<VariablePath, Lifetime>();
            this.StoredValueLifetimes = new Dictionary<VariablePath, Lifetime>();
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;

            this.LifetimeGraph = prev.LifetimeGraph;
            this.Lifetimes = prev.Lifetimes;

            this.LocationLifetimes = new StackedDictionary<VariablePath, Lifetime>(prev.LocationLifetimes);
            this.StoredValueLifetimes = new StackedDictionary<VariablePath, Lifetime>(prev.StoredValueLifetimes);
        }

        public IEnumerable<Lifetime> ReduceRootSet(IEnumerable<Lifetime> roots) {
            var result = new List<Lifetime>();

            foreach (var root in roots) {
                if (roots.Where(x => x != root).All(x => !this.LifetimeGraph.DoesOutlive(x, root))) {
                    result.Add(root);
                }
            }

            return result;
        }

        public IEnumerable<Lifetime> GetRoots(Lifetime lifetime) {
            var roots = this
                .LifetimeGraph
                .GetOutlivedLifetimes(lifetime)
                .Where(x => x.Kind != LifetimeRole.Alias);

            roots = this.ReduceRootSet(roots);

            return roots;
        }

        public void DeclareInferredLocationLifetimes(
            IdentifierPath basePath, 
            HelixType baseType, 
            TokenLocation loc,
            ValueSet<Lifetime> allowedRoots) {

            foreach (var (relPath, type) in baseType.GetMembers(this)) {
                var memPath = basePath.AppendMember(relPath);

                // Even though the lifetime of the variable itself will be inferred, the lifetime
                // of the value stored in that variable is NOT inferred. 
                var locationLifetime = new InferredLocationLifetime(loc, memPath, allowedRoots);

                // Add this variable lifetimes to the current frame
                this.LocationLifetimes[memPath] = locationLifetime;
            }
        }

        public void DeclareValueLifetimes(IdentifierPath basePath, HelixType baseType, LifetimeBundle assignBundle, LifetimeRole role) {
            foreach (var (relPath, type) in baseType.GetMembers(this)) {
                var memPath = basePath.AppendMember(relPath);

                // Even though the lifetime of the variable itself will be inferred, the lifetime
                // of the value stored in that variable is NOT inferred. 
                var valueLifetime = new ValueLifetime(memPath, role, 0);

                // Add a dependency between whatever is being assigned to this variable and the
                // variable's value
                this.LifetimeGraph.RequireOutlives(
                    assignBundle[relPath],
                    valueLifetime);

                // Both directions are required because these lifetimes are equivalent. Skipping
                // this introduces bugs when storing things into pointers
                this.LifetimeGraph.RequireOutlives(
                    valueLifetime,
                    assignBundle[relPath]);

                // Add this variable lifetimes to the current frame
                this.StoredValueLifetimes[memPath] = valueLifetime;
            }
        }
    }
}