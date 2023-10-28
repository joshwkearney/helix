using Helix.Parsing;

namespace Helix.Analysis.Flow {
    public class LifetimeException : HelixException {
        public LifetimeException(TokenLocation location, string title, string message) : base(location, title, message) { }
    }
}