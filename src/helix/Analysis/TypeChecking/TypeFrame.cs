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

        public ImmutableDictionary<IdentifierPath, LocalInfo> Locals { get; set; }

        public ImmutableHashSet<Lifetime> ValidRoots { get; set; }

        // Global things
        public Dictionary<IdentifierPath, HelixType> NominalSignatures { get; }

        public DataFlowGraph DataFlowGraph { get; }

        public Dictionary<ISyntaxTree, SyntaxTag> SyntaxTags { get; }

        public TypeFrame() {
            this.Locals = ImmutableDictionary<IdentifierPath, LocalInfo>.Empty;

            this.Locals = this.Locals.Add(
                new IdentifierPath("void"),
                new LocalInfo(PrimitiveType.Void));

            this.Locals = this.Locals.Add(
                new IdentifierPath("int"),
                new LocalInfo(PrimitiveType.Int));

            this.Locals = this.Locals.Add(
                new IdentifierPath("bool"),
                new LocalInfo(PrimitiveType.Bool));

            this.ValidRoots = ImmutableHashSet<Lifetime>.Empty;
            this.NominalSignatures = new Dictionary<IdentifierPath, HelixType>();
            this.Scope = new IdentifierPath();
            this.DataFlowGraph = new DataFlowGraph();
            this.SyntaxTags = new Dictionary<ISyntaxTree, SyntaxTag>();
        }

        private TypeFrame(TypeFrame prev) {
            this.Scope = prev.Scope;
            this.DataFlowGraph = prev.DataFlowGraph;

            this.SyntaxTags = prev.SyntaxTags;
            this.NominalSignatures = prev.NominalSignatures;

            this.Locals = prev.Locals;
            this.ValidRoots = prev.ValidRoots;
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