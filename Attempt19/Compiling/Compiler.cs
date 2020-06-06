using Attempt19.Parsing;
using Attempt19.TypeChecking;
using System.Linq;
using System.Text;

namespace Attempt19.Compiling {
    public partial class Compiler {
        public string Compile(string input) {
            // Initialize things
            var tokens = new Lexer(input).GetTokens();
            var trees = new Parser(tokens).Parse();
            var initialPath = new IdentifierPath();
            var names = new NameCache();
            var types = new TypeCache();
            var codeGen = new CodeGenerator();

            // Mature the syntax tree
            trees = trees.Select(x => x.DeclareNames(initialPath, names)).ToArray();
            trees = trees.Select(x => x.ResolveNames(names)).ToArray();
            trees = trees.Select(x => x.DeclareTypes(types)).ToArray();
            trees = trees.Select(x => x.ResolveTypes(types, null)).ToArray();

            // Generate code
            var lines = trees
                .Select(x => x.GenerateCode(codeGen))
                .SelectMany(x => x.SourceLines)
                .ToArray();

            // Get the source text
            var source = new StringBuilder();

            source.AppendLine("#include <stdlib.h>");
            source.AppendLine("#include <stdint.h>");
            source.AppendLine("#include \"regions.h\"");
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

            foreach (var line in lines) {
                source.AppendLine(line);
            }

            return source.ToString();
        }
    }
}