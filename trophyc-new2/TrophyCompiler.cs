using Trophy.Parsing;
using System.Linq;
using Trophy.Analysis;
using System.Text.RegularExpressions;
using System;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;

namespace Trophy {
    public partial class TrophyCompiler {
        private readonly string input;

        public TrophyCompiler(string input) {
            this.input = input.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
        }

        public string Compile() {
            var lexer = new Lexer(this.input);
            var parser = new Parser(lexer.GetTokens());
            var names = new NamesRecorder();
            var types = new TypesRecorder();
            var writer = new CWriter();

            // ToList() is after each step so lazy evaluation doesn't mess
            // up the order of the steps

            try {
                var parseStats = parser.Parse();
                var emptyScope = new IdentifierPath();

                foreach (var stat in parseStats) {
                    stat.DeclareNames(emptyScope, names);
                }

                foreach (var stat in parseStats) {
                    stat.DeclareTypes(emptyScope, names, types);
                }

                var stats = parseStats.Select(x => x.ResolveTypes(emptyScope, names, types)).ToArray();

                foreach (var stat in stats) {
                    stat.GenerateCode(writer);
                }

                return writer.ToString();
            }
            catch (TrophyException ex) {
                Console.WriteLine(ex.CreateConsoleMessage(this.input));

                return "";
            }
        }
    }
}
