using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Generation;
using Helix.Parsing;
using Helix.Collections;
using System.Collections.Immutable;
using Helix.Analysis.Predicates;
using Helix.Features.Types;

namespace Helix.Analysis.TypeChecking {
    public enum VariableCaptureKind {
        ValueCapture, LocationCapture
    }

    public record struct VariableCapture(IdentifierPath VariablePath, VariableCaptureKind Kind, PointerType Signature) { }

    public class TypeFrame {
        private int tempCounter = 0;

        // Frame-specific things
        public IdentifierPath Scope { get; }

        public ImmutableDictionary<IdentifierPath, LocalInfo> LocalValues { get; set; }

        public ImmutableHashSet<Lifetime> LifetimeRoots { get; set; }


        // Global things
        public Dictionary<IdentifierPath, HelixType> NominalSignatures { get; }

        public DataFlowGraph DataFlowGraph { get; }

        public Dictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }

        public Dictionary<ISyntaxTree, ISyntaxPredicate> Predicates { get; }

        public Dictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, LifetimeBounds> SyntaxLifetimes { get; }

        public TypeFrame() {
            this.LocalValues = ImmutableDictionary<IdentifierPath, LocalInfo>.Empty;

            this.LocalValues = this.LocalValues.Add(
                new IdentifierPath("void"),
                new LocalInfo(PrimitiveType.Void));

            this.LocalValues = this.LocalValues.Add(
                new IdentifierPath("int"),
                new LocalInfo(PrimitiveType.Int));

            this.LocalValues = this.LocalValues.Add(
                new IdentifierPath("bool"),
                new LocalInfo(PrimitiveType.Bool));

            this.Scope = new IdentifierPath();
            this.DataFlowGraph = new DataFlowGraph();

            this.ReturnTypes = new Dictionary<ISyntaxTree, HelixType>();
            this.CapturedVariables = new Dictionary<ISyntaxTree, IReadOnlyList<VariableCapture>>();
            this.Predicates = new Dictionary<ISyntaxTree, ISyntaxPredicate>();
            this.NominalSignatures = new Dictionary<IdentifierPath, HelixType>();

            this.SyntaxLifetimes = new Dictionary<ISyntaxTree, LifetimeBounds>();
            this.LifetimeRoots = ImmutableHashSet<Lifetime>.Empty;
        }

        private TypeFrame(TypeFrame prev) {
            this.Scope = prev.Scope;
            this.DataFlowGraph = prev.DataFlowGraph;

            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;
            this.Predicates = prev.Predicates;
            this.NominalSignatures = prev.NominalSignatures;

            this.SyntaxLifetimes = prev.SyntaxLifetimes;
            this.LocalValues = prev.LocalValues;
            this.LifetimeRoots = prev.LifetimeRoots;
        }

        public TypeFrame(TypeFrame prev, string scopeSegment) : this(prev) {
            this.Scope = prev.Scope.Append(scopeSegment);
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
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