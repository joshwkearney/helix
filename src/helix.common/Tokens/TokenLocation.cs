using System.Diagnostics.CodeAnalysis;

namespace Helix.Common.Tokens {
    public readonly record struct TokenLocation {
        public required int StartIndex { get; init; }

        public required int Length { get; init; }

        public required int Line { get; init; }

        [SetsRequiredMembers]
        public TokenLocation(int index, int length, int line) {
            this.StartIndex = index;
            this.Length = length;
            this.Line = line;
        }

        public TokenLocation Span(TokenLocation other) {
            if (other.StartIndex < this.StartIndex) {
                return other.Span(this);
            }
            else if (other.StartIndex == this.StartIndex) {
                return new TokenLocation() {
                    StartIndex = this.StartIndex,
                    Length = Math.Max(this.Length, other.Length),
                    Line = Math.Min(this.Line, other.Line)
                };
            }
            else {
                return new TokenLocation() {
                    StartIndex = this.StartIndex,
                    Length = other.StartIndex - this.StartIndex + other.Length,
                    Line = this.Line
                };
            }
        }

        public override string ToString() {
            return $"Location(Index= {this.StartIndex}, Length= {this.Length}, Line= {this.Line})";
        }
    }
}