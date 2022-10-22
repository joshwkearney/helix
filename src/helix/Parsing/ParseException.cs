namespace Helix.Parsing {
    public class ParseException : HelixException {
        public ParseException(TokenLocation location, string title, string message) : base(location, title, message) { }
    }
}