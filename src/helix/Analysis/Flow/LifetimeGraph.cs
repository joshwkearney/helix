using Helix.Analysis.Types;
using Helix.Collections;
using System.Collections.Immutable;

namespace Helix.Analysis.Flow {
    public class LifetimeGraph {
        private enum NodeRelationship {
            Dependence, Equality
        }

        private record Edge(Lifetime Lifetime, HelixType DataType, NodeRelationship EdgeKind) { }

        private readonly IDictionary<Lifetime, ISet<Edge>> outlivesGraph;
        private readonly IDictionary<Lifetime, ISet<Edge>> reverseOutlivesGraph;

        public LifetimeGraph() {
            this.outlivesGraph = new Dictionary<Lifetime, ISet<Edge>>()
                .ToDefaultDictionary(_ => new HashSet<Edge>());

            this.reverseOutlivesGraph = new Dictionary<Lifetime, ISet<Edge>>()
                .ToDefaultDictionary(_ => new HashSet<Edge>());
        }

        public void AddAssignment(Lifetime lifetime1, Lifetime lifetime2, HelixType type) {
            if (lifetime1 == Lifetime.None || lifetime2 == Lifetime.None) {
                return;
            }

            this.outlivesGraph[lifetime1].Add(new Edge(lifetime2, type, NodeRelationship.Equality));
            this.outlivesGraph[lifetime2].Add(new Edge(lifetime1, type, NodeRelationship.Equality));

            this.reverseOutlivesGraph[lifetime1].Add(new Edge(lifetime2, type, NodeRelationship.Equality));
            this.reverseOutlivesGraph[lifetime2].Add(new Edge(lifetime1, type, NodeRelationship.Equality));
        }

        public void AddStored(Lifetime lifetime1, Lifetime lifetime2, HelixType storedType) {
            if (lifetime1 == Lifetime.None || lifetime2 == Lifetime.None) {
                return;
            }

            this.outlivesGraph[lifetime1].Add(new Edge(lifetime2, storedType, NodeRelationship.Dependence));
            this.reverseOutlivesGraph[lifetime2].Add(new Edge(lifetime1, storedType, NodeRelationship.Dependence));
        }

        public IEnumerable<Lifetime> GetOutlivedLifetimes(Lifetime time) {
            return TraverseGraph(time, this.outlivesGraph, _ => true);
        }

        public IEnumerable<Lifetime> GetPrecursorLifetimes(Lifetime time) {
            return TraverseGraph(time, this.reverseOutlivesGraph, _ => true);
        }

        public IEnumerable<Lifetime> GetEquivalentLifetimes(Lifetime time) {
            return TraverseGraph(
                time, 
                this.reverseOutlivesGraph, 
                edge => edge.EdgeKind == NodeRelationship.Equality);
        }

        public bool DoesOutlive(Lifetime first, Lifetime second) {
            return this.GetOutlivedLifetimes(first).Contains(second);
        }

        private static IEnumerable<Lifetime> TraverseGraph(
            Lifetime time,
            IDictionary<Lifetime, ISet<Edge>> graph,
            Func<Edge, bool> canFollow) {

            var visited = new HashSet<Lifetime>();
            var stack = new Stack<Lifetime>(new[] { time });
            var result = new List<Lifetime>();

            while (stack.Count > 0) {
                var item = stack.Pop();

                if (visited.Contains(item) || item == Lifetime.None) {
                    continue;
                }
                else {
                    visited.Add(item);
                }

                result.Add(item);

                // Whenever we hit a lifetime that does not outlive any other lifetime,
                // we have found a root, so add it to the results
                if (graph.TryGetValue(item, out var outlivedList) && outlivedList.Any()) {
                    foreach (var edge in outlivedList) {
                        if (canFollow(edge)) {
                            stack.Push(edge.Lifetime);
                        }
                    }
                }          
            }

            return result;
        }
    }
}