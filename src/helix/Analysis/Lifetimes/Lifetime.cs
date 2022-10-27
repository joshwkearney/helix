using Helix.Parsing;
using System.IO;

namespace Helix.Analysis.Lifetimes {
    public record struct Lifetime(IdentifierPath Path, int MutationCount) {

        public Lifetime() : this(new IdentifierPath(), 0) { }        
    }
}