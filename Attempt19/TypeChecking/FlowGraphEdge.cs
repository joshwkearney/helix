using System;

namespace Attempt19.TypeChecking {
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
}