using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private class CodeGenerator : ICodeGenerator {
            private readonly SyntaxRegistry registry;
            private readonly TypeGenerator typegen;

            public CWriter Header1Writer { get; } = new CWriter();

            public CWriter Header2Writer { get; } = new CWriter();

            public CWriter Header3Writer { get; } = new CWriter();

            ICodeWriter ICodeGenerator.Header1Writer => this.Header1Writer;

            ICodeWriter ICodeGenerator.Header2Writer => this.Header2Writer;

            ICodeWriter ICodeGenerator.Header3Writer => this.Header3Writer;

            public CodeGenerator(SyntaxRegistry registry) {
                this.registry = registry;
                this.typegen = new TypeGenerator(this.Header2Writer);
            }

            public CBlock Generate(ISyntax<TypeCheckTag> syntax) {
                return this.registry.syntaxTrees[syntax.GetType()](syntax, this);
            }

            public string Generate(LanguageType type) {
                return type.Accept(this.typegen);
            }
        }
    }
}