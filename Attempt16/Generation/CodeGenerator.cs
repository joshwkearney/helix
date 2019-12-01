using Attempt16.Analysis;
using Attempt16.Syntax;
using System.Text;
using System.Text.RegularExpressions;

namespace Attempt16.Generation {
    public class CodeGenerator {
        public CompilationUnit CompilationUnit { get; }

        public Scope Scope { get; }

        public CodeGenerator(CompilationUnit unit, Scope scope) {
            this.CompilationUnit = unit;
            this.Scope = scope;
        }

        public (string source, string header) Generate() {
            // Build the source file
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine($"#include \"{this.CompilationUnit.FileName}.h\"");
            sourceBuilder.AppendLine("");

            // Build the header file
            string name = new Regex(@"[^a-zA-Z0-9]").Replace(this.CompilationUnit.FileName, "$").ToUpper() + "_H";
            var headerBuilder = new StringBuilder();

            headerBuilder.AppendLine("#include \"Language.h\"");
            headerBuilder.AppendLine();
            headerBuilder.AppendLine("#ifndef " + name);
            headerBuilder.AppendLine("#define " + name);
            headerBuilder.AppendLine();

            // First find struct forward declarations
            var declgen = new StructForwardDeclarationGenerator();
           
            foreach (var stat in this.CompilationUnit.Declarations) {
                var code = stat.Accept(declgen);

                foreach (var line in code.HeaderLines) {
                    headerBuilder.AppendLine(line);
                }
            }

            var typegen = new TypeGenerator(this.Scope);
            var expressionProvider = new ExpressionCGProvider(typegen);
            var declarationProvider = new DeclarationCGProvider(expressionProvider, typegen);

            foreach (var stat in this.CompilationUnit.Declarations) {
                var code = stat.Accept(declarationProvider).Generate(stat);

                foreach (var line in code.SourceLines) {
                    sourceBuilder.AppendLine(line);
                }

                foreach (var line in code.HeaderLines) {
                    headerBuilder.AppendLine(line);
                }
            }

            headerBuilder.AppendLine("#endif");

            return (sourceBuilder.ToString(), headerBuilder.ToString());
        }
    }
}