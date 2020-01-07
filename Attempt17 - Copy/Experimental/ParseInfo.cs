using Attempt17.Parsing;

namespace Attempt17.Experimental {
    public class ParseInfo {
        public TokenLocation Location { get; }

        public ParseInfo(TokenLocation loc) {
            this.Location = loc;
        }
    }
}