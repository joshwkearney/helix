using Helix.Parsing;
using System.IO;

namespace Helix.Analysis.Lifetimes {
    // The mutation count serves to distinguish lifetimes from different versions of the
    // same mutated variable. A root lifetime is a lifetime that describes a variable 
    // whose mutation will have effects that escape the current function scope. Parameters,
    // the implicit heap, and newly dereferenced reference types are all root lifetimes,
    // along with any locals that depend on root lifetimes.
    public record struct Lifetime(IdentifierPath Path, int MutationCount) {
        public static Lifetime None { get; } = new Lifetime(new IdentifierPath(), 0);

        public Lifetime() : this(new IdentifierPath(), 0) { }        
    }
}