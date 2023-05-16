using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;

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
        public IDictionary<IdentifierPath, Lifetime> VariableLifetimes { get; }

        public IDictionary<IdentifierPath, Lifetime> VariableValueLifetimes { get; }

        public FlowFrame(TypeFrame frame) {
            ReturnTypes = frame.ReturnTypes;
            Variables = frame.Variables;
            Functions = frame.Functions;
            Structs = frame.Structs;

            LifetimeGraph = new();
            Lifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();

            VariableLifetimes = new Dictionary<IdentifierPath, Lifetime>();
            VariableValueLifetimes = new Dictionary<IdentifierPath, Lifetime>();
        }

        public FlowFrame(FlowFrame prev) {
            ReturnTypes = prev.ReturnTypes;
            Variables = prev.Variables;
            Functions = prev.Functions;
            Structs = prev.Structs;

            LifetimeGraph = prev.LifetimeGraph;
            Lifetimes = prev.Lifetimes;

            VariableLifetimes = new StackedDictionary<IdentifierPath, Lifetime>(prev.VariableLifetimes);
            VariableValueLifetimes = new StackedDictionary<IdentifierPath, Lifetime>(prev.VariableValueLifetimes);
        }

        public IEnumerable<Lifetime> ReduceRootSet(IEnumerable<Lifetime> roots) {
            var result = new List<Lifetime>();

            foreach (var root in roots) {
                if (!roots.Where(x => x != root).Any(x => this.LifetimeGraph.DoesOutlive(x, root))) {
                    result.Add(root);
                }
            }

            return result;
        }

        public ValueList<Lifetime> GetRoots(IdentifierPath lifetimePath) {
            var roots = this
                .LifetimeGraph
                .GetOutlivedLifetimes(this.VariableLifetimes[lifetimePath])
                .Where(x => x.Kind != LifetimeKind.Inferencee);

            roots = this.ReduceRootSet(roots);

            return roots.ToValueList();
        }
    }
}