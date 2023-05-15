using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Helix.Analysis.Lifetimes {
    public class LifetimeGraph {
        private readonly Dictionary<Lifetime, ISet<Lifetime>> outlivesGraph = new();
        private readonly Dictionary<Lifetime, ISet<Lifetime>> reverseOutlivesGraph = new();

        public void RequireOutlives(Lifetime lifetime, Lifetime outlivedLifetime) {
            if (lifetime == Lifetime.None || outlivedLifetime == Lifetime.None) {
                return;
            }

            if (lifetime == outlivedLifetime) {
                return;
            }

            if (!this.outlivesGraph.TryGetValue(lifetime, out var outlivedList)) {
                this.outlivesGraph[lifetime] = outlivedList = new HashSet<Lifetime>();
            }

            if (!this.reverseOutlivesGraph.TryGetValue(outlivedLifetime, out var outlivingList)) {
                this.reverseOutlivesGraph[outlivedLifetime] = outlivingList = new HashSet<Lifetime>();
            }

            outlivedList.Add(outlivedLifetime);
            outlivingList.Add(lifetime);
        }

        public IEnumerable<Lifetime> GetOutlivedLifetimes(Lifetime time) {
            return TraverseGraph(time, this.outlivesGraph);
        }

        public IEnumerable<Lifetime> GetPrecursorLifetimes(Lifetime time) {
            return TraverseGraph(time, this.reverseOutlivesGraph);
        }

        public bool DoesOutlive(Lifetime first, Lifetime second) {
            return this.GetOutlivedLifetimes(first).Contains(second);
        }

        public static IEnumerable<Lifetime> TraverseGraph(
            Lifetime time,
            Dictionary<Lifetime, ISet<Lifetime>> graph) {

            var visited = new HashSet<Lifetime>();
            var stack = new Stack<Lifetime>(new[] { time });

            while (stack.Count > 0) {
                var item = stack.Pop();

                if (visited.Contains(item) || item == Lifetime.None) {
                    continue;
                }
                else {
                    visited.Add(item);
                }

                yield return item;

                // Whenever we hit a lifetime that does not outlive any other lifetime,
                // we have found a root, so add it to the results
                if (graph.TryGetValue(item, out var outlivedList) && outlivedList.Any()) {
                    foreach (var child in outlivedList) {
                        stack.Push(child);
                    }
                }          
            }
        }
    }
}