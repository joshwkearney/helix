using System.Diagnostics.CodeAnalysis;

namespace Helix.Common.Tokens {
    public readonly record struct Token {
        public required TokenLocation Location { get; init; }

        public required TokenKind Kind { get; init; }

        public required string Value { get; init; }

        [SetsRequiredMembers]
        public Token(TokenKind kind, TokenLocation loc, string value) {
            this.Kind = kind;
            this.Location = loc;
            this.Value = value;
        }

        public override string ToString() {
            return $"Token(Kind= {this.Kind}, Value= {this.Value}, Location= {this.Location.StartIndex})";
        }
    }
}