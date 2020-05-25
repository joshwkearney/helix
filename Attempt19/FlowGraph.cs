using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Attempt19 {
    public class FlowGraphEdge : IEquatable<FlowGraphEdge> {
        public IdentifierPath CapturedVariable { get; }

        public IdentifierPath DependentVariable { get; }

        public VariableCaptureKind CapturedKind { get; }

        public FlowGraphEdge(IdentifierPath captured, IdentifierPath dependent, VariableCaptureKind kind) {
            this.CapturedVariable = captured;
            this.DependentVariable = dependent;
            this.CapturedKind = kind;
        }

        public override int GetHashCode() {
            return this.CapturedVariable.GetHashCode() 
                + 7 * this.DependentVariable.GetHashCode() 
                + 11 * this.CapturedKind.GetHashCode();
        }

        public override bool Equals(object other) {
            if (other == null) {
                return false;
            }

            if (other is FlowGraphEdge edge) {
                return this.Equals(edge);
            }

            return false;
        }

        public bool Equals(FlowGraphEdge other) {
            if (other == null) {
                return false;
            }

            return other.CapturedKind == this.CapturedKind
                && other.CapturedVariable == this.CapturedVariable
                && other.DependentVariable == this.DependentVariable;
        }
    }

    public class FlowGraph {
        private readonly ImmutableHashSet<FlowGraphEdge> edges;

        public FlowGraph() {
            this.edges = ImmutableHashSet.Create<FlowGraphEdge>();
        }

        private FlowGraph(ImmutableHashSet<FlowGraphEdge> edges) {
            this.edges = edges;
        }

        //public IEnumerable<IdentifierPath> FindDirectCapturedVariables(IdentifierPath dependentVariable, params VariableCaptureKind[] edgeKind) {
        //    return this.edges
        //        .Where(x => x.DependentVariable == dependentVariable)
        //        .Where(x => edgeKind.Contains(x.CapturedKind))
        //        .Select(x => x.CapturedVariable);
        //}

        //public IEnumerable<IdentifierPath> FindDirectDependentVariables(IdentifierPath capturedVariable, params VariableCaptureKind[] edgeKind) {
        //    return this.edges
        //        .Where(x => x.CapturedVariable == capturedVariable)
        //        .Where(x => edgeKind.Contains(x.CapturedKind))
        //        .Select(x => x.DependentVariable);
        //}

        public IEnumerable<VariableCapture> FindAllCapturedVariables(IdentifierPath dependentVariable) {
            return this.FindAllCapturedVariables(dependentVariable, VariableCaptureKind.MoveCapture, 
                VariableCaptureKind.ReferenceCapture, VariableCaptureKind.ValueCapture);
        }

        public IEnumerable<VariableCapture> FindAllCapturedVariables(IdentifierPath dependentVariable, params VariableCaptureKind[] edgeKind) {
            var visited = new HashSet<IdentifierPath>();
            var toVisit = new Queue<IdentifierPath>();

            toVisit.Enqueue(dependentVariable);

            while (toVisit.Count > 0) {
                var next = toVisit.Dequeue();

                var edges = this.edges
                    .Where(x => x.DependentVariable == next)
                    .Where(x => edgeKind.Contains(x.CapturedKind))
                    .ToArray();

                foreach (var edge in edges) {
                    yield return new VariableCapture(edge.CapturedKind, edge.CapturedVariable);

                    if (!visited.Contains(edge.CapturedVariable)) {
                        toVisit.Enqueue(edge.CapturedVariable);
                    }
                }
            }
        }

        public IEnumerable<VariableCapture> FindAllDependentVariables(IdentifierPath dependentVariable) {
            return this.FindAllDependentVariables(dependentVariable, VariableCaptureKind.MoveCapture,
                VariableCaptureKind.ReferenceCapture, VariableCaptureKind.ValueCapture);
        }

        public IEnumerable<VariableCapture> FindAllDependentVariables(IdentifierPath capturedVariable, params VariableCaptureKind[] edgeKind) {
            var visited = new HashSet<IdentifierPath>();
            var toVisit = new Queue<IdentifierPath>();

            toVisit.Enqueue(capturedVariable);

            while (toVisit.Count > 0) {
                var next = toVisit.Dequeue();

                var edges = this.edges
                    .Where(x => x.CapturedVariable == next)
                    .Where(x => edgeKind.Contains(x.CapturedKind))
                    .ToArray();

                foreach (var edge in edges) {
                    yield return new VariableCapture(edge.CapturedKind, edge.DependentVariable);

                    if (!visited.Contains(edge.DependentVariable)) {
                        toVisit.Enqueue(edge.DependentVariable);
                    }
                }
            }
        }

        //public IEnumerable<IdentifierPath> FindAllDependentVariables(IdentifierPath capturedVariable, params VariableCaptureKind[] edgeKind) {
        //    var visited = new HashSet<IdentifierPath>();
        //    var toVisit = new Queue<IdentifierPath>();

        //    toVisit.Enqueue(capturedVariable);

        //    while (toVisit.Count > 0) {
        //        var next = toVisit.Dequeue();

        //        yield return next;

        //        var neighbors = this.edges
        //            .Where(x => x.CapturedVariable == next)
        //            .Where(x => edgeKind.Contains(x.CapturedKind))
        //            .Select(x => x.DependentVariable)
        //            .ToArray();

        //        foreach (var neighbor in neighbors) {
        //            if (!visited.Contains(neighbor)) {
        //                toVisit.Enqueue(neighbor);
        //            }
        //        }
        //    }
        //}

        public FlowGraph AddEdge(IdentifierPath captured, IdentifierPath dependent, VariableCaptureKind kind) {
            var newEdges = this.edges.Add(new FlowGraphEdge(captured, dependent, kind));
           
            return new FlowGraph(newEdges);
        }
    }
}