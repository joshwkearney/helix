namespace Helix.Analysis.Flow {
    public enum LifetimeRole {
        Alias, Root
    }

    public enum LifetimeSubject {
        Location, StoredValue
    }

    // The mutation count serves to distinguish lifetimes from different versions of the
    // same mutated variable. A root lifetime is a lifetime that describes a variable 
    // whose mutation will have effects that escape the current function scope. Parameters,
    // the implicit heap, and newly dereferenced reference types are all root lifetimes,
    // along with any locals that depend on root lifetimes.
    public record struct Lifetime(VariablePath Path, int Version, LifetimeSubject Target, LifetimeRole Kind) {
        public static Lifetime Heap { get; } = new Lifetime(
            new IdentifierPath("$heap").ToVariablePath(), 
            0,
            LifetimeSubject.Location,
            LifetimeRole.Root);
        
        public static Lifetime None { get; } = new Lifetime(
            new IdentifierPath("$none").ToVariablePath(),
            0,
            LifetimeSubject.Location,
            LifetimeRole.Root);

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