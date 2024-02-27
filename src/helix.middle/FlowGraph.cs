using Helix.HelixMinusMinus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd {
    internal class FlowGraph<TEdge> {
        private readonly HashSet<FlowGraphNode> nodes = [];
        private readonly Dictionary<FlowGraphNode, Dictionary<FlowGraphNode, TEdge>> edges = [];
        private readonly Dictionary<FlowGraphNode, Dictionary<FlowGraphNode, TEdge>> reverseEdges = [];

        public void AddEdge(FlowGraphNode fromNode, FlowGraphNode toNode, TEdge edge) {
            this.nodes.Add(fromNode);
            this.nodes.Add(toNode);

            if (!this.edges.ContainsKey(fromNode)) {
                this.edges[fromNode] = [];
            }

            if (!this.reverseEdges.ContainsKey(toNode)) {
                this.reverseEdges[toNode] = [];
            }

            this.edges[fromNode][toNode] = edge;
            this.reverseEdges[toNode][fromNode] = edge;
        }        

        public FlowGraphNeighbors<TEdge> NextNeighbors(FlowGraphNode node) {
            var edges = this.edges.GetValueOrNone(node).OrElse(() => []).ToDictionary(x => x.Key, x => x.Value);

            return new FlowGraphNeighbors<TEdge>(edges);
        }

        public FlowGraphNeighbors<TEdge> PreviousNeighbors(FlowGraphNode node) {
            var edges = this.reverseEdges.GetValueOrNone(node).OrElse(() => []).ToDictionary(x => x.Key, x => x.Value);

            return new FlowGraphNeighbors<TEdge>(edges);
        }
    }

    public class FlowGraphNode {
        public IHmmSyntax Statement { get; init; }

        public FlowGraphNode(IHmmSyntax stat) {
            this.Statement = stat;
        }
    }

    public class FlowGraphNeighbors<TEdge> {
        public IReadOnlyDictionary<FlowGraphNode, TEdge> Edges { get; init; }

        public IReadOnlyList<FlowGraphNode> Neighbors => this.Edges.Keys.ToArray();

        public bool IsLeaf => this.Edges.Count == 0;

        public FlowGraphNeighbors(IReadOnlyDictionary<FlowGraphNode, TEdge> edges) {
            this.Edges = edges;
        }
    }
}
