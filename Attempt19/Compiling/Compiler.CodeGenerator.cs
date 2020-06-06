using Attempt19.CodeGeneration;
using Attempt19.Types;
using System;

namespace Attempt19.Compiling {
    public partial class Compiler {
        private class CodeGenerator : ICodeGenerator {
            private readonly TypeGenerator typegen;

            public CWriter Header1Writer { get; } = new CWriter();

            public CWriter Header2Writer { get; } = new CWriter();

            public CWriter Header3Writer { get; } = new CWriter();

            ICodeWriter ICodeGenerator.Header1Writer => this.Header1Writer;

            ICodeWriter ICodeGenerator.Header2Writer => this.Header2Writer;

            ICodeWriter ICodeGenerator.Header3Writer => this.Header3Writer;

            public CodeGenerator() {
                this.typegen = new TypeGenerator(this.Header1Writer, this.Header2Writer);
            }

            public string Generate(LanguageType type) {
                return type.Accept(this.typegen);
            }
        }
    }
}