using Attempt17.CodeGeneration;
using Attempt17.Features;
using Attempt17.Features.FlowControl;
using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Features.Variables;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private readonly ILanguageFeature[] features = new ILanguageFeature[] { 
            new FlowControlFeature(),
            new FunctionsFeature(),
            new PrimitivesFeature(),
            new VariablesFeature()
        };

        public CompilerResult Compile(string input) {
            var registry = this.GetRegistry();
            var tokens = new Lexer(input).GetTokens();
            var decls = new Parser(tokens).Parse();
            var codegen = new CodeGenerator(registry);
            var scope = new Scope();

            // Make sure all the declarations can add to the scope
            foreach (var decl in decls) {
                registry.declarations[decl.GetType()](decl, scope);
            }

            var typeChecker = new TypeChecker(registry, scope);

            // Type check everything
            var checkedDecls = decls
                .Select(x => typeChecker.Check(x, scope))
                .ToArray();

            // Generate everything
            var lines = checkedDecls
                .Select(codegen.Generate)
                .SelectMany(x => x.SourceLines)
                .ToArray();

            // Get the header text
            var header = new StringBuilder();

            foreach (var line in codegen.Header1Writer.ToLines()) {
                header.AppendLine(line);
            }

            foreach (var line in codegen.Header2Writer.ToLines()) {
                header.AppendLine(line);
            }

            foreach (var line in codegen.Header3Writer.ToLines()) {
                header.AppendLine(line);
            }

            // Get the source text
            var source = new StringBuilder();

            foreach (var line in lines) {
                source.AppendLine(line);
            }

            return new CompilerResult(header.ToString(), source.ToString());
        }

        private SyntaxRegistry GetRegistry() {
            var registry = new SyntaxRegistry();

            foreach (var feature in this.features) {
                feature.RegisterSyntax(registry);
            }

            return registry;
        }
    }
}