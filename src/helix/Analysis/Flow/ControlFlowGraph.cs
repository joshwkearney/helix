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
    public abstract record IFlowControlNode {
        public static IFlowControlNode Start { get; } = new LeafNode(true);

        public static IFlowControlNode End { get; } = new LeafNode(false);

        public static IFlowControlNode None { get; } = new NoneNode();

        public static IFlowControlNode FromScope(IdentifierPath scope) {
            return new ScopeNode(scope);
        }

        public virtual bool IsStart => false;

        public virtual bool IsEnd => false;

        public virtual Option<IdentifierPath> AsScope() => Option.None;

        private record ScopeNode(IdentifierPath Scope) : IFlowControlNode {
            public Option<IdentifierPath> AsScope() => this.Scope;
        }

        private record LeafNode(bool IsStartProp) : IFlowControlNode {
            public override bool IsStart => this.IsStartProp;

            public override bool IsEnd => !this.IsStartProp;

            public override string ToString() {
                if (this.IsStartProp) {
                    return "[start node]";
                }
                else {
                    return "[end node]";
                }
            }
        }

        private record NoneNode : IFlowControlNode { }
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

        public void AddEdge(IdentifierPath parent, IdentifierPath child) {
            this.AddEdge(parent, child, ISyntaxPredicate.Empty);
        }

        public void AddEdge(IdentifierPath parent, IdentifierPath child, ISyntaxPredicate pred) {
            this.AddEdge(
                IFlowControlNode.FromScope(parent),
                IFlowControlNode.FromScope(child),
                pred);
        }

        public void AddEdge(IdentifierPath parent, IFlowControlNode child) {
            this.AddEdge(
                IFlowControlNode.FromScope(parent),
                child,
                ISyntaxPredicate.Empty);
        }

        public void AddEdge(IFlowControlNode parent, IFlowControlNode child, ISyntaxPredicate pred) {
            if (parent == IFlowControlNode.None || child == IFlowControlNode.None) {
                return;
            }

            this.edges[parent][child] = pred;
            this.reverse_edges[child][parent] = pred;
        }

        public void AddContinuation(IdentifierPath stat, IdentifierPath sibling) {
            this.AddContinuation(stat, IFlowControlNode.FromScope(sibling));
        }

        public void AddContinuation(IdentifierPath stat, IFlowControlNode sibling) {
            if (sibling == IFlowControlNode.None) {
                return;
            }

            this.continuations.Add(stat, sibling);
        }

        public IFlowControlNode GetContinuation(IdentifierPath stat) {
            return this.continuations
                .GetValueOrNone(stat)
                .OrElse(() => IFlowControlNode.None);
        }

        public ISyntaxPredicate GetPredicates(IdentifierPath path) {
            var startNode = IFlowControlNode.Start;
            var endNode = IFlowControlNode.FromScope(path);

            var result = this.GetPathPredicate(startNode, endNode);

            //var reachable = this.FindReachableNodes(endNode, this.reverse_edges);
            //var unreachable = this.edges.Keys.Except(reachable).ToHashSet();

            //foreach (var unreach in unreachable) {
            //    var pred = this.GetPathPredicate(startNode, unreach).Negate();

            //    result = result.And(pred);
            //}

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
            var paths = this.GetAllPaths(start, end);

            foreach (var path in paths) {
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

        private HashSet<IFlowControlNode> FindReachableNodes(
            IFlowControlNode start,
            IDictionary<IFlowControlNode, IDictionary<IFlowControlNode, ISyntaxPredicate>> graph) {

            var visited = new HashSet<IFlowControlNode>();
            var stack = new Stack<IFlowControlNode>(new[] { start });
            var result = new HashSet<IFlowControlNode>();

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
