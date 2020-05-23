using Attempt18.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Attempt18.CodeGeneration {
    public class CBlock {
        public ImmutableList<string> SourceLines { get; }

        public string Value { get; }

        public CBlock(string returnValue) {
            this.Value = returnValue;
            this.SourceLines = ImmutableList<string>.Empty;
        }

        public CBlock(string returnValue, ImmutableList<string> sourceLines) {
            this.Value = returnValue;
            this.SourceLines = sourceLines;
        }

        public CBlock Combine(CBlock other, Func<string, string, string> combiner) {
            return new CBlock(
                combiner(this.Value, other.Value),
                this.SourceLines.AddRange(other.SourceLines));
        }
    }
}