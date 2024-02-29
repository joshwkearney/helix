using Helix.Common;
using Helix.Common.Tokens;

namespace Helix.Frontend.NameResolution {
    internal class NameResolutionException : HelixException {
        public NameResolutionException(TokenLocation location, string title, string message) : base(location, title, message) { }

        public static NameResolutionException IdentifierUndefined(TokenLocation location, string name) {
            return new NameResolutionException(
                location,
                "Analysis Exception: Identifier Undefined",
                $"The identifier '{name}' is not defined in the current scope");
        }

        public static NameResolutionException IdentifierDefined(TokenLocation location, string name) {
            return new NameResolutionException(
                location,
                "Analysis Exception: Identifier Already Defined",
                $"The identifier '{name}' is already defined in the current scope");
        }
    }
}