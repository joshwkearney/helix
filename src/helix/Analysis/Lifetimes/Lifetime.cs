using Helix.Parsing;
using System.IO;

namespace Helix.Analysis.Lifetimes {
    public enum LifetimeKind {
        Root, Inferencee, Passthrough
    }

    // The mutation count serves to distinguish lifetimes from different versions of the
    // same mutated variable. A root lifetime is a lifetime that describes a variable 
    // whose mutation will have effects that escape the current function scope. Parameters,
    // the implicit heap, and newly dereferenced reference types are all root lifetimes,
    // along with any locals that depend on root lifetimes.
    public record struct Lifetime(IdentifierPath Path, int Version, LifetimeKind Kind) {
        public static Lifetime Heap { get; } = new Lifetime(new IdentifierPath("$heap"), 0, LifetimeKind.Root);

        public static Lifetime Stack { get; } = new Lifetime(new IdentifierPath("$stack"), 0, LifetimeKind.Root);

        public static Lifetime None { get; } = new Lifetime(new IdentifierPath("$none"), 0, LifetimeKind.Passthrough);

        public Lifetime() : this(new IdentifierPath(), 0, LifetimeKind.Passthrough) { }        
    }
}