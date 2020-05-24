using Attempt19.Parsing;
using System.Text;

namespace Attempt19.Compiling {
    public partial class Compiler {
        public string Compile(string input) {
            // Initialize things
            var tokens = new Lexer(input).GetTokens();
            var tree = new Parser(tokens).Parse();
            var initialPath = new IdentifierPath();
            var names = new NameCache();
            var types = new TypeCache();
            var flows = new FlowCache() {
                CapturedVariables = new ImmutableGraph<IdentifierPath>(),
                DependentVariables = new ImmutableGraph<IdentifierPath>() };
            var codeGen = new CodeGenerator(null);

            // Mature the syntax tree
            tree = tree.DeclareNames(initialPath, names);
            tree = tree.ResolveNames(names);
            tree = tree.DeclareTypes(types);
            tree = tree.ResolveTypes(types);
            tree = tree.AnalyzeFlow(flows);

            // Generate code
            var block = tree.GenerateCode(null, codeGen);

            // Get the source text
            var source = new StringBuilder();

            source.AppendLine("#include <stdlib.h>");
            source.AppendLine("#include <stdint.h>");
            source.AppendLine("");

            foreach (var line in codeGen.Header1Writer.ToLines()) {
                source.AppendLine(line);
            }

            foreach (var line in codeGen.Header2Writer.ToLines()) {
                source.AppendLine(line);
            }

            foreach (var line in codeGen.Header3Writer.ToLines()) {
                source.AppendLine(line);
            }

            foreach (var line in block.SourceLines) {
                source.AppendLine(line);
            }

            return source.ToString();
        }
    }
}