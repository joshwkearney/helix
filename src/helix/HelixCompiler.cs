using Helix.Parsing;
using Helix.Generation;
using Helix.Analysis.TypeChecking;

namespace Helix {
    public class HelixCompiler {
        private readonly string header;
        private readonly string input;

        public HelixCompiler(string header, string input) {          
            this.header = header;
            this.input = input;
        }

        public string Compile() {
            var input = this.input
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\t", "    ");

            try {
                // ToList() is after each step so lazy evaluation doesn't mess
                // up the order of the steps
                var parser = new Parser(input);
                var types = new TypeFrame();
                var parseStats = parser.Parse();

                foreach (var stat in parseStats) {
                    types = stat.DeclareNames(types);
                }

                foreach (var stat in parseStats) {
                    types = stat.DeclareTypes(types);
                }
                
                var writer = new CWriter(this.header);

                foreach (var parseStat in parseStats) {
                    var (stat, statTypes) = parseStat.CheckTypes(types);
                    
                    stat.GenerateCode(statTypes, writer);
                }

                return writer.ToString();
            }
            catch (HelixException ex) {
                var newMessage = ex.CreateConsoleMessage(input);
                var newEx = new HelixException(ex.Location, ex.Title, newMessage);

                throw newEx;
            }
        }
    }
}
