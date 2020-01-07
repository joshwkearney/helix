using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class CodeGenerator {
        private int tempCounter = 0;
        private readonly List<string> headerLines = new List<string>();

        public IReadOnlyList<string> HeaderLines => this.headerLines;

        public HashSet<LanguageType> GeneratedTypes { get; } = new HashSet<LanguageType>();

        public ICodeWriter GetHeaderWriter() {
            return new HeaderWriter(this);
        }

        public string GetTempVariableName() {
            return "$temp" + this.tempCounter++;
        }

        private class HeaderWriter : ICodeWriter {
            private readonly CodeGenerator gen;

            public HeaderWriter(CodeGenerator gen) {
                this.gen = gen;
            }

            public ICodeWriter Line(string line) {
                this.gen.headerLines.Add(line);
                return this;
            }
        }
    }
}