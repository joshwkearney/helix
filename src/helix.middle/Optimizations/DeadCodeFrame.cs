namespace Helix.MiddleEnd.Optimizations {
    internal class DeadCodeFrame {
        private readonly HashSet<string> UsedVariables = new HashSet<string>();
        private readonly HashSet<string> UsedVariablesSinceAssignment = new HashSet<string>();

        public DeadCodeFrame() { }

        private DeadCodeFrame(HashSet<string> used, HashSet<string> usedSinceAssignment) {
            this.UsedVariables = used;
            this.UsedVariablesSinceAssignment = usedSinceAssignment;
        }

        public DeadCodeFrame CreateScope() {
            return new DeadCodeFrame(this.UsedVariables.ToHashSet(), this.UsedVariablesSinceAssignment.ToHashSet());
        }

        public DeadCodeFrame MergeWith(DeadCodeFrame other) {
            return new DeadCodeFrame(
                this.UsedVariables.Concat(other.UsedVariables).ToHashSet(), 
                this.UsedVariablesSinceAssignment.Concat(other.UsedVariablesSinceAssignment).ToHashSet());
        }

        public void UseVariable(string variable) {
            if (int.TryParse(variable, out _) || bool.TryParse(variable, out _) || variable == "void") {
                return;
            }

            this.UsedVariables.Add(variable);
            this.UsedVariablesSinceAssignment.Add(variable);
        }

        public void AssignTo(string variable) {
            if (int.TryParse(variable, out _) || bool.TryParse(variable, out _) || variable == "void") {
                return;
            }

            this.UsedVariablesSinceAssignment.Remove(variable);
        }

        public bool CanRemoveVariable(string variable) {
            return !this.UsedVariables.Contains(variable);
        }

        public bool CanRemoveAssignment(string variable) {
            return !this.UsedVariablesSinceAssignment.Contains(variable);
        }
    }
}
