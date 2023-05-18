namespace Helix.Analysis.Flow {
    public enum LifetimeRole {
        Inference, Relational
    }

    public enum LifetimeTarget {
        Location, StoredValue
    }

    // The mutation count serves to distinguish lifetimes from different versions of the
    // same mutated variable. A root lifetime is a lifetime that describes a variable 
    // whose mutation will have effects that escape the current function scope. Parameters,
    // the implicit heap, and newly dereferenced reference types are all root lifetimes,
    // along with any locals that depend on root lifetimes.
    public record struct Lifetime(VariablePath Path, int Version, LifetimeTarget Target, LifetimeRole Kind) {
        public static Lifetime Heap { get; } = new Lifetime(
            new IdentifierPath("$heap").ToVariablePath(), 
            0,
            LifetimeTarget.Location,
            LifetimeRole.Relational);
        
        public static Lifetime None { get; } = new Lifetime(
            new IdentifierPath("$none").ToVariablePath(),
            0,
            LifetimeTarget.Location,
            LifetimeRole.Relational);

        public override string ToString() {
            if (this == Heap) {
                return "return_region";
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