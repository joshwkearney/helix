using Trophy.Parsing;
using Trophy.Analysis;
using Trophy.Generation;

namespace Trophy {
    public class TrophyCompiler {
        private readonly string input;

        public TrophyCompiler(string input) {
            this.input = input.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
        }

        public string Compile() {
            // ToList() is after each step so lazy evaluation doesn't mess
            // up the order of the steps

            try {
                var lexer = new Lexer(this.input);
                var parser = new Parser(lexer.GetTokens());
                var names = new NamesRecorder();
                var types = new TypesRecorder(names);
                var writer = new CWriter(names.TypeDeclarations);

                var parseStats = parser.Parse();

                foreach (var stat in parseStats) {
                    stat.DeclareNames(names);
                }

                foreach (var stat in parseStats) {
                    stat.DeclareTypes(types);
                }

                var stats = parseStats.Select(x => x.CheckTypes(types)).ToArray();

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
