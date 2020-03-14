using Attempt17.CodeGeneration;
using Attempt17.Features;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private class CodeGenerator : ICodeGenerator {
            private readonly TypeGenerator typegen;
            private readonly TypeDestructorGenerator destructGen;

            public CWriter Header1Writer { get; } = new CWriter();

            public CWriter Header2Writer { get; } = new CWriter();

            public CWriter Header3Writer { get; } = new CWriter();

            ICodeWriter ICodeGenerator.Header1Writer => this.Header1Writer;

            ICodeWriter ICodeGenerator.Header2Writer => this.Header2Writer;

            ICodeWriter ICodeGenerator.Header3Writer => this.Header3Writer;

            public CodeGenerator(ICScope outerScope) {
                this.typegen = new TypeGenerator(outerScope, this.Header2Writer);
                this.destructGen = new TypeDestructorGenerator(this.Header3Writer, this, outerScope);
            }

            public string Generate(LanguageType type) {
                return type.Accept(this.typegen);
            }

            public IOption<string> GetDestructor(LanguageType type) {
                return type.Accept(this.destructGen);
            }

            public CBlock CopyValue(string value, LanguageType type,
                CodeGenerationContext context) {

                return type.Accept(new ValueCopyVisitor(value, this, context.Scope));
            }
        }
    }
}