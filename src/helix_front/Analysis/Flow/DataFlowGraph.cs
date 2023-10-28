using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using System.Collections.Immutable;

namespace Helix.Analysis.Flow {
    public class DataFlowGraph {
        private enum NodeRelationship {
            Dependence, Equality, Member
        }

        private record Edge(Lifetime Lifetime, NodeRelationship EdgeKind, HelixType type) { }

        private readonly IDictionary<Lifetime, ISet<Edge>> outlivesGraph;
        private readonly IDictionary<Lifetime, ISet<Edge>> reverseOutlivesGraph;

        public DataFlowGraph() {
            this.outlivesGraph = new Dictionary<Lifetime, ISet<Edge>>()
                .ToDefaultDictionary(_ => new HashSet<Edge>());

            this.reverseOutlivesGraph = new Dictionary<Lifetime, ISet<Edge>>()
                .ToDefaultDictionary(_ => new HashSet<Edge>());
        }

        public void Print() {
            foreach (var (node, edges) in this.outlivesGraph) {
                foreach (var edge in edges) {
                    Console.Write(node.Path.Segments.Last() + ", " + node.Origin + " -> ");
                    Console.WriteLine(edge.Lifetime.Path.Segments.Last() + ", " + edge.Lifetime.Origin);
                }
            }
        }

        public void AddAssignment(Lifetime lifetime1, Lifetime lifetime2, HelixType type) {
            if (lifetime1 == Lifetime.None || lifetime2 == Lifetime.None || lifetime1 == lifetime2) {
                return;
            }

            this.outlivesGraph[lifetime1].Add(new Edge(lifetime2, NodeRelationship.Equality, type));
            this.outlivesGraph[lifetime2].Add(new Edge(lifetime1, NodeRelationship.Equality, type));

            this.reverseOutlivesGraph[lifetime1].Add(new Edge(lifetime2, NodeRelationship.Equality, type));
            this.reverseOutlivesGraph[lifetime2].Add(new Edge(lifetime1, NodeRelationship.Equality, type));
        }

        public void AddStored(Lifetime lifetime1, Lifetime lifetime2, HelixType type) {
            if (lifetime1 == Lifetime.None || lifetime2 == Lifetime.None || lifetime1 == lifetime2) {
                return;
            }

            this.outlivesGraph[lifetime1].Add(new Edge(lifetime2, NodeRelationship.Dependence, type));
            this.reverseOutlivesGraph[lifetime2].Add(new Edge(lifetime1, NodeRelationship.Dependence, type));
        }

        public void AddMember(Lifetime parent, Lifetime member, HelixType memType) {
            if (parent == Lifetime.None || member == Lifetime.None || parent == member) {
                return;
            }

            this.outlivesGraph[parent].Add(new Edge(member, NodeRelationship.Member, memType));
            this.reverseOutlivesGraph[member].Add(new Edge(parent, NodeRelationship.Member, memType));
        }


        public IEnumerable<Lifetime> GetOutlivedLifetimes(Lifetime time) {
            static bool canFollow(Edge edge) => false
                || edge.EdgeKind == NodeRelationship.Equality 
                || edge.EdgeKind == NodeRelationship.Dependence;

            return TraverseGraph(time, this.outlivesGraph, canFollow);
        }

        public IEnumerable<Lifetime> GetPrecursorLifetimes(Lifetime time) {
            static bool canFollow(Edge edge) => false
                || edge.EdgeKind == NodeRelationship.Equality
                || edge.EdgeKind == NodeRelationship.Dependence;

            return TraverseGraph(time, this.reverseOutlivesGraph, canFollow);
        }

        public IEnumerable<Lifetime> GetAliasedLifetimes(Lifetime time, HelixType type, TypeFrame types) {
            bool canFollow(Edge edge) {
                var goodEdge = false
                    || edge.EdgeKind == NodeRelationship.Equality
                    || edge.EdgeKind == NodeRelationship.Dependence;

                var goodType = edge.type
                    .GetAccessibleTypes(types)
                    .Any(x => x.CanUnifyTo(type, types));

                return goodEdge && goodType;
            }

            return TraverseGraph(time, this.reverseOutlivesGraph, canFollow);
        }

        public IEnumerable<Lifetime> GetEquivalentLifetimes(Lifetime time) {
            return TraverseGraph(
                time, 
                this.outlivesGraph, 
                edge => edge.EdgeKind == NodeRelationship.Equality);
        }

        public IEnumerable<Lifetime> GetMemberLifetimes(Lifetime time, string memberName) {
            var result = this.GetEquivalentLifetimes(time)
                .Where(x => this.outlivesGraph.ContainsKey(x))
                .Select(x => this.outlivesGraph[x]);

            var result2 = result
                .SelectMany(x => x.Where(y => y.EdgeKind == NodeRelationship.Member))
                .Select(x => x.Lifetime)
                .Where(x => x.Path.Segments.Last() == memberName)
                .ToArray();

            return result2;
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