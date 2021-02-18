using Attempt20.Analysis;
using Attempt20.Compiling;
using Attempt20.Parsing;
using System;
using System.Linq;

namespace Attempt20 {
    public partial class TrophyCompiler {
        private readonly string input;

        public TrophyCompiler(string input) {
            this.input = input.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        public string Compile() {
            var lexer = new Lexer(this.input);
            var parser = new Parser(lexer.GetTokens());
            var names = new NamesRecorder();
            var types = new TypesRecorder();
            var writer = new CWriter();
            var declarations = parser.Parse();

            // Declare the heap and the stack for all code
            names.DeclareGlobalName(new IdentifierPath("heap"), NameTarget.Region);
            names.DeclareGlobalName(new IdentifierPath("stack"), NameTarget.Region);

            foreach (var decl in declarations) {
                decl.DeclareNames(names);
            }

            foreach (var decl in declarations) {
                decl.ResolveNames(names);
            }

            foreach (var decl in declarations) {
                decl.DeclareTypes(names, types);
            }

            foreach (var decl in declarations) {
                var tree = decl.ResolveTypes(names, types);
                tree.GenerateCode(writer);
            }

            return writer.ToString();
        }
    }
}
