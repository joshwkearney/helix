using Helix.Analysis.Predicates;
using Helix.Collections;
using Helix.Generation.Syntax;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Flow {
    public interface IFlowControlNode {
        public static IFlowControlNode Start { get; } = new LeafNode(true);

        public static IFlowControlNode End { get; } = new LeafNode(false);

        public static IFlowControlNode FromScope(IdentifierPath scope) {
            return new ScopeNode(scope);
        }

        public bool IsStart => false;

        public bool IsEnd => false;

        public Option<IdentifierPath> AsScope() => Option.None;

        private record ScopeNode(IdentifierPath Scope) : IFlowControlNode {
            public Option<IdentifierPath> AsScope() => this.Scope;
        }

        private record LeafNode(bool IsStartProp) : IFlowControlNode {
            public bool IsStart => this.IsStartProp;

            public bool IsEnd => !this.IsStartProp;

            public override string ToString() {
                if (this.IsStartProp) {
                    return "[start node]";
                }
                else {
                    return "[end node]";
                }
            }
        }
    }

    public class ControlFlowGraph {
        private readonly DefaultDictionary<IFlowControlNode, IDictionary<IFlowControlNode, ISyntaxPredicate>> edges;
        private readonly DefaultDictionary<IFlowControlNode, IDictionary<IFlowControlNode, ISyntaxPredicate>> reverse_edges;
        private readonly Dictionary<IdentifierPath, IFlowControlNode> continuations;

        public ControlFlowGraph() {
            this.edges = new DefaultDictionary<IFlowControlNode, IDictionary<IFlowControlNode, ISyntaxPredicate>>(
                _ => new DefaultDictionary<IFlowControlNode, ISyntaxPredicate>(
                    _ => ISyntaxPredicate.Empty));

            this.reverse_edges = new DefaultDictionary<IFlowControlNode, IDictionary<IFlowControlNode, ISyntaxPredicate>>(
                _ => new DefaultDictionary<IFlowControlNode, ISyntaxPredicate>(
                    _ => ISyntaxPredicate.Empty));

            this.continuations = new Dictionary<IdentifierPath, IFlowControlNode>();
        }

        public void AddEndingEdge(IdentifierPath scope) {
            var node = IFlowControlNode.FromScope(scope);
            var end = IFlowControlNode.End;

            this.edges[node][end] = ISyntaxPredicate.Empty;
            this.reverse_edges[end][node] = ISyntaxPredicate.Empty;
        }

        public void AddStartingEdge(IdentifierPath scope) {
            var node = IFlowControlNode.FromScope(scope);
            var start = IFlowControlNode.Start;

            this.edges[start][node] = ISyntaxPredicate.Empty;
            this.reverse_edges[node][start] = ISyntaxPredicate.Empty;
        }

        public void AddEdge(IdentifierPath parent, IdentifierPath child) {
            this.AddEdge(parent, IFlowControlNode.FromScope(child), ISyntaxPredicate.Empty);
        }

        public void AddEdge(IdentifierPath parent, IFlowControlNode child) {
            this.AddEdge(parent, child, ISyntaxPredicate.Empty);
        }

        public void AddEdge(IdentifierPath parent, IFlowControlNode child, ISyntaxPredicate pred) {
            var parentNode = IFlowControlNode.FromScope(parent);

            this.edges[parentNode][child] = pred;
            this.reverse_edges[child][parentNode] = pred;
        }

        public void SetContinuation(IdentifierPath stat, IdentifierPath sibling) {
            this.continuations[stat] = IFlowControlNode.FromScope(sibling);
        }

        public void SetContinuation(IdentifierPath stat, IFlowControlNode sibling) {
            this.continuations[stat] = sibling;
        }

        public void AddEndingContinutation(IdentifierPath stat) {
            this.continuations[stat] = IFlowControlNode.End;
        }

        public bool TryGetContinuation(IdentifierPath stat, out IFlowControlNode result) {
            return this.continuations
                .GetValueOrNone(stat)
                .TryGetValue(out result);
        }

        public ISyntaxPredicate GetPredicates(IdentifierPath start, IdentifierPath end) {
            var startNode = IFlowControlNode.FromScope(start);
            var endNode = IFlowControlNode.FromScope(end);

            var result = this.GetPathPredicate(startNode, endNode);
            //var negative = this.GetPathPredicate(start, this.end).Negate();

            return result;
        }

        public bool AlwaysReturns(IdentifierPath tree) {
            var node = IFlowControlNode.FromScope(tree);

            return this.AlwaysReturnsHelper(node, new HashSet<IFlowControlNode>());
        }

        private bool AlwaysReturnsHelper(IFlowControlNode tree, HashSet<IFlowControlNode> visited) {
            visited.Add(tree);

            if (tree == IFlowControlNode.End) {
                return true;
            }

            if (this.edges.TryGetValue(tree, out var edges)) {
                if (edges.Any()) {
                    var alwaysReturns = true;

                    foreach (var (child, _) in this.edges[tree]) {
                        if (!visited.Contains(child)) {
                            alwaysReturns &= AlwaysReturnsHelper(child, visited);
                        }
                    }

                    return alwaysReturns;
                }
            }

            return false;
        }

        private ISyntaxPredicate GetPathPredicate(IFlowControlNode start, IFlowControlNode end) {
            var result = ISyntaxPredicate.Empty;

            foreach (var path in this.GetAllPaths(start, end)) {
                var pred = ISyntaxPredicate.Empty;

                for (int i = 0; i < path.Count - 1; i++) {
                    var first = path[i];
                    var second = path[i + 1];

                    pred = pred.And(this.edges[first][second]);
                }

                result = result.Or(pred);
            }

            return result;
        }

        private HashSet<ValueList<IFlowControlNode>> GetAllPaths(IFlowControlNode start, IFlowControlNode end) {
            var stack = new Stack<ValueList<IFlowControlNode>>(new[] { new[] { start }.ToValueList() });
            var result = new HashSet<ValueList<IFlowControlNode>>();

            while (stack.Count > 0) {
                var path = stack.Pop();
                var last = path[path.Count - 1];

                if (result.Contains(path)) {
                    continue;
                }

                foreach (var (child, _) in this.edges[last]) {
                    if (path.Contains(child)) {
                        continue;
                    }

                    var newPath = path.Add(child);

                    if (child == end) {
                        result.Add(newPath);
                    }
                    else {
                        stack.Push(newPath);
                    }
                }
            }

            return result;
        }

        private HashSet<object> FindReachableNodes(
            object start,
            IDictionary<object, IDictionary<object, ISyntaxPredicate>> graph) {

            var visited = new HashSet<object>();
            var stack = new Stack<object>(new[] { start });
            var result = new HashSet<object>();

            while (stack.Count > 0) {
                var item = stack.Pop();

                if (visited.Contains(item)) {
                    continue;
                }
                else {
                    visited.Add(item);
                }

                result.Add(item);

                // Whenever we hit a lifetime that does not outlive any other lifetime,
                // we have found a root, so add it to the results
                if (graph.TryGetValue(item, out var outlivedList) && outlivedList.Any()) {
                    foreach (var child in outlivedList.Keys) {
                        stack.Push(child);
                    }
                }
            }

            return result;
        }
    }
}
