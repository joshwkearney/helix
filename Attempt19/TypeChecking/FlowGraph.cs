using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Attempt19.TypeChecking {
    public class FlowGraph {
        private readonly ImmutableHashSet<FlowGraphEdge> edges;

        public FlowGraph() {
            this.edges = ImmutableHashSet.Create<FlowGraphEdge>();
        }

        private FlowGraph(ImmutableHashSet<FlowGraphEdge> edges) {
            this.edges = edges;
        }

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

        public FlowGraph AddEdge(IdentifierPath captured, IdentifierPath dependent, VariableCaptureKind kind) {
            var newEdges = this.edges.Add(new FlowGraphEdge(captured, dependent, kind));
           
            return new FlowGraph(newEdges);
        }
    }
}