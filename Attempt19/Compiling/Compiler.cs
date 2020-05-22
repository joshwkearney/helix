using Attempt18.CodeGeneration;
using Attempt18.Features;
using Attempt18.Parsing;
using Attempt18.TypeChecking;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt18.Compiling {
    public partial class Compiler {
        public string Compile(string input) {
            var tokens = new Lexer(input).GetTokens();

            var scope = new OuterScope();
            var declFlattener = new DeclarationFlattener(scope);
            var declScoper = new DeclarationScoper();

            var decls = new Parser(tokens)
                .Parse()
                .SelectMany(x => x.Accept(declFlattener, scope))
                .Select(x => x.Accept(declScoper, scope))
                .ToArray();

            var typeChecker = new SyntaxTypeChecker();
            var typeCheckContext = new TypeCheckContext(new TypeChecker(), scope);

            // Type check everything
            var checkedDecls = decls
                .Select(x => x.Accept(typeChecker, typeCheckContext))
                .ToArray();

            var cscope = new OuterCScope(scope.TypeInfo);
            var codeGen = new CodeGenerator(cscope);
            var syntaxCodegen = new SyntaxCodeGenerator();
            var codegenContext = new CodeGenerationContext(cscope, codeGen);

            // Generate everything
            var lines = checkedDecls
                .Select(x => x.Accept(syntaxCodegen, codegenContext))
                .SelectMany(x => x.SourceLines)
                .ToImmutableList();


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

            foreach (var line in lines) {
                source.AppendLine(line);
            }

            return source.ToString();
        }
    }
}