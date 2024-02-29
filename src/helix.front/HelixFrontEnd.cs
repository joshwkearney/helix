using Helix.Common.Hmm;
using Helix.Frontend.NameResolution;
using Helix.Frontend.ParseTree;

namespace Helix.Frontend {
    public class HelixFrontend {
        public IReadOnlyList<IHmmSyntax> Compile(string text) {
            var parser = new Parser(text);
            var parseTree = parser.Parse();

            var declarations = new DeclarationStore();
            var declarationFinder = new DeclarationFinder(declarations);
            
            foreach (var tree in parseTree) {
                tree.Accept(declarationFinder);
            }

            var nameResolver = new NameResolver(declarations);

            foreach (var tree in parseTree) {
                tree.Accept(nameResolver);
            }

            return nameResolver.Result;
        }

        public string CompileToString(string text) {
            var hmm = this.Compile(text);
            var stringifier = new HmmStringifier();
            var result = "";

            foreach (var line in hmm) {
                result += line.Accept(stringifier);
            }

            return result;
        }
    }
}
