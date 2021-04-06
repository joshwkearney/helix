using Trophy.Parsing;

namespace Trophy.Analysis {
    public class TypeCheckingException : TrophyException {
        public TypeCheckingException(TokenLocation location, string title, string message) : base(location, title, message) { }
    }
}