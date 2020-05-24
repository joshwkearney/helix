using Attempt17.CodeGeneration;
using Attempt19.CodeGeneration;
using Attempt19.Types;
using System;

namespace Attempt19.Compiling {
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
                this.typegen = new TypeGenerator(outerScope, this.Header1Writer, this.Header2Writer);
                this.destructGen = new TypeDestructorGenerator(this.Header2Writer, this);
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

            public CBlock MoveValue(string value, LanguageType type) {
                return type.Accept(new ValueMoveVisitor(value, this));
            }
        }
    }
}