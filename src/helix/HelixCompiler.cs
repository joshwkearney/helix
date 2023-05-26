using Helix.Parsing;
using Helix.Generation;
using Helix.Analysis.Flow;
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
                var writer = new CWriter(this.header, types.TypeDeclarations);
                var parseStats = parser.Parse();

                foreach (var stat in parseStats) {
                    stat.DeclareNames(types);
                }

                foreach (var stat in parseStats) {
                    stat.DeclareTypes(types);
                }

                var stats = parseStats.Select(x => x.CheckTypes(types)).ToArray();

                // We need to declare the flow frame down here after types is done being
                // modified, because it uses immutable dictionaries
                var flow = new FlowFrame(types);

                foreach (var stat in stats) {
                    stat.AnalyzeFlow(flow);
                }

                foreach (var stat in stats) {
                    stat.GenerateCode(flow, writer);
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
