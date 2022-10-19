using Trophy.Parsing;
using Trophy.Analysis;
using Trophy.Generation;

namespace Trophy {
    public class TrophyCompiler {
        private readonly string header;
        private readonly string input;

        public TrophyCompiler(string header, string input) {          
            this.header = header;
            this.input = input;
        }

        public string Compile() {
            // ToList() is after each step so lazy evaluation doesn't mess
            // up the order of the steps

            var input = this.input
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\t", "    ");

            try {
                var lexer = new Lexer(input);
                var parser = new Parser(lexer.GetTokens());
               // var names = new NamesRecorder();
                var types = new SyntaxFrame();
                var writer = new CWriter(this.header, types.TypeDeclarations);

                var parseStats = parser.Parse();

                foreach (var stat in parseStats) {
                    stat.DeclareNames(types);
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
                Console.WriteLine(ex.CreateConsoleMessage(input));

                return "";
            }
        }
    }
}
