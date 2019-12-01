using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt4 {
    public enum CompileExceptionCategory {
        Syntactic,
        Semantic
    }

    public class CompileException : Exception {

        public CompileExceptionCategory Category { get; }

        public int TextPosition { get; }

        public CompileException(CompileExceptionCategory cat, int pos, string message) : base(message) {
            this.Category = cat;
            this.TextPosition = pos;
        }
    }
}