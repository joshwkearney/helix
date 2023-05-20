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
        public IDictionary<VariablePath, LifetimeBounds> VariableLifetimes { get; }

        public FlowFrame(TypeFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.Variables = frame.Variables;
            this.Functions = frame.Functions;
            this.Structs = frame.Structs;

            this.LifetimeGraph = new();
            this.Lifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            this.VariableLifetimes = new DefaultDictionary<VariablePath, LifetimeBounds>(_ => LifetimeBounds.Empty);
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;

            this.LifetimeGraph = prev.LifetimeGraph;
            this.Lifetimes = prev.Lifetimes;

            this.VariableLifetimes = prev.VariableLifetimes
                .ToStackedDictionary()
                .ToDefaultDictionary(_ => LifetimeBounds.Empty);
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
                .Where(x => x.Role != LifetimeRole.Alias)
                .Where(x => x != lifetime);

            roots = this.ReduceRootSet(roots);

            return roots;
        }

        public void DeclareInferredLocationLifetimes(
            IdentifierPath basePath, 
            HelixType baseType, 
            TokenLocation loc,
            ValueSet<Lifetime> allowedRoots) {

            foreach (var (relPath, _) in baseType.GetMembers(this)) {
                var memPath = basePath.AppendMember(relPath);
                var locationLifetime = new InferredLocationLifetime(loc, memPath, allowedRoots);

                // Add this variable lifetimes to the current frame
                this.VariableLifetimes[memPath] = this.VariableLifetimes[memPath].WithLValue(locationLifetime);
            }
        }

        public void DeclareValueLifetimes(IdentifierPath basePath, HelixType baseType, LifetimeBundle assignBundle, LifetimeRole role) {
            foreach (var (relPath, _) in baseType.GetMembers(this)) {
                var memPath = basePath.AppendMember(relPath);
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
                this.VariableLifetimes[memPath] = this.VariableLifetimes[memPath].WithRValue(valueLifetime);
            }
        }
    }
}