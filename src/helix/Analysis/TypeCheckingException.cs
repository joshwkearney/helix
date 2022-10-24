using Helix.Parsing;

namespace Helix.Analysis {
    public class TypeCheckingException : HelixException {
        public TypeCheckingException(TokenLocation location, string title, string message) : base(location, title, message) { }
    }

    public class LifetimeException : HelixException {
        public LifetimeException(TokenLocation location, string title, string message) : base(location, title, message) { }
    }
}