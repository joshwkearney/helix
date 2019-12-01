using System.Collections.Generic;
using System.Collections.Immutable;

namespace Attempt16.Generation {
    public class CCode {
        public ImmutableList<string> SourceLines { get; }

        public ImmutableList<string> HeaderLines { get; }

        public string Value { get; }

        public CCode(string returnValue) {
            this.Value = returnValue;
            this.SourceLines = ImmutableList<string>.Empty;
            this.HeaderLines = ImmutableList<string>.Empty;
        }

        public CCode(string returnValue, ImmutableList<string> sourceLines, ImmutableList<string> headerLines) {
            this.Value = returnValue;
            this.SourceLines = sourceLines;
            this.HeaderLines = headerLines;
        }

        public CCode(string returnValue, IEnumerable<string> sourceLines, IEnumerable<string> headerLines) {
            this.Value = returnValue;
            this.SourceLines = sourceLines.ToImmutableList();
            this.HeaderLines = headerLines.ToImmutableList();
        }
    }
}