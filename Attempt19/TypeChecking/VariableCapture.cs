using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Attempt19.TypeChecking {
    public enum VariableCaptureKind {
        MoveCapture, ValueCapture, ReferenceCapture
    }

    public class VariableCapture : IEquatable<VariableCapture> {
        public IdentifierPath VariablePath { get; }

        public VariableCaptureKind Kind { get; }

        public VariableCapture(VariableCaptureKind kind, IdentifierPath varPath) {
            this.VariablePath = varPath;
            this.Kind = kind;
        }

        public override int GetHashCode() {
            return this.Kind.GetHashCode() + 7 * this.VariablePath.GetHashCode();
        }

        public override bool Equals(object other) {
            if (other == null) {
                return false;
            }

            if (other is VariableCapture cap) {
                return this.Equals(cap);
            }

            return false;
        }

        public bool Equals(VariableCapture other) {
            if (other == null) {
                return false;
            }

            return other.Kind == this.Kind
                && other.VariablePath == this.VariablePath;
        }
    }
}