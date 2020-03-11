using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private class CodeGenerator : ICodeGenerator {
            private readonly SyntaxRegistry registry;
            private readonly TypeGenerator typegen;
            private readonly TypeDestructorGenerator destructGen;

            public CWriter Header1Writer { get; } = new CWriter();

            public CWriter Header2Writer { get; } = new CWriter();

            public CWriter Header3Writer { get; } = new CWriter();

            ICodeWriter ICodeGenerator.Header1Writer => this.Header1Writer;

            ICodeWriter ICodeGenerator.Header2Writer => this.Header2Writer;

            ICodeWriter ICodeGenerator.Header3Writer => this.Header3Writer;

            public CodeGenerator(SyntaxRegistry registry, ICScope outerScope) {
                this.registry = registry;
                this.typegen = new TypeGenerator(this.Header2Writer);
                this.destructGen = new TypeDestructorGenerator(this.Header3Writer, this, outerScope);
            }

            public CBlock Generate(ISyntax<TypeCheckTag> syntax, ICScope scope) {
                return this.registry.syntaxTrees[syntax.GetType()](syntax, scope, this);
            }

            public string Generate(LanguageType type) {
                return type.Accept(this.typegen);
            }

            public IOption<string> GetDestructor(LanguageType type) {
                return type.Accept(this.destructGen);
            }

            public CBlock CopyValue(string value, LanguageType type, ICScope scope) {
                return type.Accept(new ValueCopyVisitor(value, this, scope));
            }
        }
    }
}