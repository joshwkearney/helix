namespace Trophy.Parsing {
    public record struct TokenLocation {
        public int StartIndex { get; }

        public int Length { get; }

        public int Line { get; }

        public TokenLocation(int start, int length, int line) {
            this.StartIndex = start;
            this.Length = length;
            this.Line = line;
        }

        public TokenLocation Span(TokenLocation other) {
            if (other.StartIndex < this.StartIndex) {
                return other.Span(this);
            }
            else if (other.StartIndex == this.StartIndex) {
                return new TokenLocation(
                    this.StartIndex, 
                    Math.Max(this.Length, other.Length),
                    Math.Min(this.Line, other.Line));
            }
            else {
                return new TokenLocation(
                    this.StartIndex, 
                    other.StartIndex - this.StartIndex + other.Length,
                    this.Line);
            }
        }
    }
}