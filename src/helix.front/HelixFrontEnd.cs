using helix.front.NameResolution;
using helix.front.Parsing;
using Helix.HelixMinusMinus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.front {
    public class HelixFrontEnd {
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
    }
}
