using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using System.Security.AccessControl;

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
            ReturnTypes = frame.ReturnTypes;
            Variables = frame.Variables;
            Functions = frame.Functions;
            Structs = frame.Structs;

            LifetimeGraph = new();
            Lifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            LocationLifetimes = new Dictionary<VariablePath, Lifetime>();
            StoredValueLifetimes = new Dictionary<VariablePath, Lifetime>();
        }

        public FlowFrame(FlowFrame prev) {
            ReturnTypes = prev.ReturnTypes;
            Variables = prev.Variables;
            Functions = prev.Functions;
            Structs = prev.Structs;

            LifetimeGraph = prev.LifetimeGraph;
            Lifetimes = prev.Lifetimes;

            LocationLifetimes = new StackedDictionary<VariablePath, Lifetime>(prev.LocationLifetimes);
            StoredValueLifetimes = new StackedDictionary<VariablePath, Lifetime>(prev.StoredValueLifetimes);
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
                .Where(x => x.Kind != LifetimeRole.Inference);

            roots = this.ReduceRootSet(roots);

            return roots;
        }

        public void DeclareLocationLifetimes(IdentifierPath basePath, HelixType baseType, LifetimeRole role) {
            foreach (var (relPath, type) in baseType.GetMembers(this)) {
                var memPath = basePath.AppendMember(relPath);

                // Even though the lifetime of the variable itself will be inferred, the lifetime
                // of the value stored in that variable is NOT inferred. 
                var locationLifetime = new Lifetime(memPath, 0, LifetimeTarget.Location, role);

                // Add this variable lifetimes to the current frame
                this.LocationLifetimes[memPath] = locationLifetime;
            }
        }

        public void DeclareValueLifetimes(IdentifierPath basePath, HelixType baseType, LifetimeBundle assignBundle, LifetimeRole role) {
            foreach (var (relPath, type) in baseType.GetMembers(this)) {
                var memPath = basePath.AppendMember(relPath);

                // Even though the lifetime of the variable itself will be inferred, the lifetime
                // of the value stored in that variable is NOT inferred. 
                var valueLifetime = new Lifetime(memPath, 0, LifetimeTarget.StoredValue, role);

                // Add a dependency between whatever is being assigned to this variable and the
                // variable's value
                this.LifetimeGraph.RequireOutlives(
                    assignBundle[relPath],
                    valueLifetime);

                // Add this variable lifetimes to the current frame
                this.StoredValueLifetimes[memPath] = valueLifetime;
            }
        }
    }
}