using Attempt16.Analysis;
using Attempt16.Generation;
using Attempt16.Parsing;
using Attempt16.Syntax;
using System.IO;
using System.Reflection;

namespace Attempt16 {
    public class Compiler {
        public void Compile(string inputDirectory, string outputDirectory) {
            // Copy the language header file
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Attempt15.Language.h";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                File.WriteAllText(Path.Combine(outputDirectory, "Language.h"), result);
            }

            // Compile each file
            foreach (var file in Directory.GetFiles(inputDirectory, "*.txt", SearchOption.AllDirectories)) {
                var text = File.ReadAllText(file);

                var tokens = new Lexer(text).GetTokens();
                var decls = new Parser(tokens).Parse();

                string relPath = Path.ChangeExtension(Path.GetRelativePath(inputDirectory, file), null);

                var unit = new CompilationUnit() {
                    Declarations = decls,
                    FileName = relPath
                };

                var typeChecker = new TypeChecker(unit);
                var scope = typeChecker.TypeCheck();

                var codeGen = new CodeGenerator(unit, scope);
                var (source, header) = codeGen.Generate();

                string sourcePath = Path.Combine(outputDirectory, Path.ChangeExtension(relPath, "c"));
                string headerPath = Path.Combine(outputDirectory, Path.ChangeExtension(relPath, "h"));

                Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
                Directory.CreateDirectory(Path.GetDirectoryName(headerPath));

                File.WriteAllText(sourcePath, source);
                File.WriteAllText(headerPath, header);
            }
        }
    }
}
