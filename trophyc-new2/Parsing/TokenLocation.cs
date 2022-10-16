namespace Trophy.Parsing {
    public struct TokenLocation {
        public int StartIndex { get; }

        public int Length { get; }

        public TokenLocation(int start, int length) {
            this.StartIndex = start;
            this.Length = length;
        }

        public TokenLocation Span(TokenLocation other) {
            if (other.StartIndex < this.StartIndex) {
                return other.Span(this);
            }
            else if (other.StartIndex == this.StartIndex) {
                return new TokenLocation(this.StartIndex, Math.Max(this.Length, other.Length));
            }
            else {
                return new TokenLocation(this.StartIndex, other.StartIndex - this.StartIndex + other.Length);
            }
        }
    }
}