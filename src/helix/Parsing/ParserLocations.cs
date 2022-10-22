using Helix.Analysis;

namespace Helix.Parsing {
    public record struct TokenLocation {
        public int StartIndex { get; }

        public int Length { get; }

        public int Line { get; }

        public IdentifierPath Scope { get; }

        public TokenLocation(int start, int length, int line, IdentifierPath scope) {
            this.StartIndex = start;
            this.Length = length;
            this.Line = line;
            this.Scope = scope;
        }

        public TokenLocation Span(TokenLocation other) {
            if (other.StartIndex < this.StartIndex) {
                return other.Span(this);
            }
            else if (other.StartIndex == this.StartIndex) {
                return new TokenLocation(
                    this.StartIndex,
                    Math.Max(this.Length, other.Length),
                    Math.Min(this.Line, other.Line),
                    this.Scope);
            }
            else {
                return new TokenLocation(
                    this.StartIndex,
                    other.StartIndex - this.StartIndex + other.Length,
                    this.Line,
                    this.Scope);
            }
        }
    }
}