using Attempt20.Analysis;
using Attempt20.Compiling;
using Attempt20.Parsing;
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

            // ToList() is after each step so lazy evaluation doesn't mess
            // up the order of the steps
            parser
                .Parse()
                .Select(x => x.DeclareNames(names)).ToList()
                .Select(x => x.ResolveNames(names)).ToList()
                .Select(x => x.DeclareTypes(types)).ToList()
                .Select(x => x.ResolveTypes(types)).ToList()
                .ForEach(x => x.GenerateCode(writer));

            return writer.ToString();
        }
    }
}
