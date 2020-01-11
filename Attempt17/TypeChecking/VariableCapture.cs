namespace Attempt17.TypeChecking {
    public class VariableCapture {
        public VariableCaptureKind Kind { get; }

        public IdentifierPath Path { get; }

        public VariableCapture(VariableCaptureKind kind, IdentifierPath path) {
            this.Kind = kind;
            this.Path = path;
        }

        public override bool Equals(object obj) {
            if (obj is null) {
                return false;
            }

            if (obj is VariableCapture var) {
                return this.Kind == var.Kind && this.Path == var.Path;
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Kind.GetHashCode() + 7 * this.Path.GetHashCode();
        }

        public static bool operator ==(VariableCapture var1, VariableCapture var2) {
            return var1?.Equals(var2) ?? false;
        }

        public static bool operator !=(VariableCapture var1, VariableCapture var2) {
            return !(var1 == var2);
        }
    }
}