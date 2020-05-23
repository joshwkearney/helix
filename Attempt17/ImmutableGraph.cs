using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17 {
    public class ImmutableGraph<T> {
        private readonly ImmutableDictionary<T, ImmutableHashSet<T>> edges;

        public ImmutableGraph() {
            this.edges = ImmutableDictionary<T, ImmutableHashSet<T>>.Empty;
        }

        private ImmutableGraph(ImmutableDictionary<T, ImmutableHashSet<T>> edges) {
            this.edges = edges;
        }

        public IReadOnlyCollection<T> GetNeighbors(T node) {
            return this.edges
                .GetValueOption(node)
                .Select(x => (IReadOnlyCollection<T>)x)
                .GetValueOr(() => new T[0]);
        }

        public IEnumerable<T> FindAccessibleNodes(T node) {
            var visited = new HashSet<T>();
            var toVisit = new Queue<T>();

            toVisit.Enqueue(node);

            while (toVisit.Count > 0) {
                var next = toVisit.Dequeue();

                yield return next;

                foreach (var neighbor in this.GetNeighbors(next)) {
                    if (!visited.Contains(neighbor)) {
                        toVisit.Enqueue(neighbor);
                    }
                }
            }
        }

        public ImmutableGraph<T> AddEdge(T node, T neighbor) {
            var newEdges = this.edges;
            
            if (newEdges.ContainsKey(node)) {
                newEdges = newEdges.SetItem(node, newEdges[node].Add(neighbor));
            }
            else {
                newEdges = newEdges.Add(node, new[] { neighbor }.ToImmutableHashSet());
            }

            return new ImmutableGraph<T>(newEdges);
        }

        public ImmutableGraph<T> RemoveEdge(T node, T neighbor) {
            var newEdges = this.edges;

            if (!newEdges.ContainsKey(node)) {
                return this;
            }

            if (!newEdges[node].Contains(neighbor)) {
                return this;
            }

            newEdges = newEdges.SetItem(node, newEdges[neighbor].Remove(neighbor));
            return new ImmutableGraph<T>(newEdges);
        }
    }
}