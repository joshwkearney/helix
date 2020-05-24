using Attempt19.Parsing;
using System;

namespace Attempt19 {
    public class CompilerException : Exception {
        public string Title { get; }

        public TokenLocation Location { get; }

        public CompilerException(TokenLocation location, string title, string message) : base(message) {
            this.Location = location;
            this.Title = title;
        }
    }
}