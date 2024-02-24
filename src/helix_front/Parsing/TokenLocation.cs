﻿using Helix.Analysis;

namespace Helix.Parsing {
    public record struct TokenLocation(int StartIndex, int Length, int Line) {
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