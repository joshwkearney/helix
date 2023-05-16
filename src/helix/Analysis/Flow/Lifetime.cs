namespace Helix.Analysis.Flow {
    public enum LifetimeKind {
        Inferencee, Other
    }

    // The mutation count serves to distinguish lifetimes from different versions of the
    // same mutated variable. A root lifetime is a lifetime that describes a variable 
    // whose mutation will have effects that escape the current function scope. Parameters,
    // the implicit heap, and newly dereferenced reference types are all root lifetimes,
    // along with any locals that depend on root lifetimes.
    public record struct Lifetime(VariablePath Path, int Version, LifetimeKind Kind = LifetimeKind.Other) {
        public static Lifetime Heap { get; } = new Lifetime(
            new VariablePath(new IdentifierPath("$heap")), 
            0);

        public static Lifetime Stack { get; } = new Lifetime(
            new VariablePath(new IdentifierPath("$stack")),
            0); 
        
        public static Lifetime None { get; } = new Lifetime(
            new VariablePath(new IdentifierPath("$none")),
            0);

        public override string ToString() {
            if (this == Heap) {
                return "return_region";
            }
            else if (this == Stack) {
                return "stack";
            }
            else if (this == None) {
                return "none";
            }
            else {
                return this.Path.ToString();
            }
        }
    }
}