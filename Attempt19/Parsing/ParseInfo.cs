namespace Attempt18.Parsing {
    public class ParseTag {
        public TokenLocation Location { get; }

        public ParseTag(TokenLocation loc) {
            this.Location = loc;
        }
    }
}