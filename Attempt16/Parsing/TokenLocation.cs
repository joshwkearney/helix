namespace Attempt16.Parsing {
    public class TokenLocation {
        public int StartIndex { get; }

        public int EndIndex { get; }

        public TokenLocation(int start, int end) {
            this.StartIndex = start;
            this.EndIndex = end;
        }

        public TokenLocation(TokenLocation start, TokenLocation end) {
            this.StartIndex = start.StartIndex;
            this.EndIndex = end.EndIndex;
        }
    }
}