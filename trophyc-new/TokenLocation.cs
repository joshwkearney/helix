using System;

namespace Trophy.Parsing {
    public struct TokenLocation {
        public readonly int startLine;
        public readonly int endLine;
        public readonly int startColumn;
        public readonly int endColumn;

        public TokenLocation(int startLine, int endLine, int startColumn, int endColumn) {
            this.startLine = startLine;
            this.endLine = endLine;
            this.startColumn = startColumn;
            this.endColumn = endColumn;
        }

        public TokenLocation Span(TokenLocation other) {
            if (this.startLine == other.startLine) {
                int startCol = Math.Min(this.startColumn, other.startColumn);

                if (this.endLine == other.endLine) {
                    int endCol = Math.Max(this.endColumn, other.endColumn);

                    return new TokenLocation(this.startLine, this.endLine, startCol, endCol);
                }
                else if (this.endLine > other.endLine) {
                    return new TokenLocation(this.startLine, this.endLine, startCol, this.endColumn);
                }
                else {
                    return new TokenLocation(this.startLine, other.endLine, startCol, other.endColumn);
                }
            }
            else if (other.startLine < this.startLine) {
                return other.Span(this);
            }
            else {
                return new TokenLocation(this.startLine, other.endLine, this.startColumn, other.endColumn);
            }
        }
    }
}