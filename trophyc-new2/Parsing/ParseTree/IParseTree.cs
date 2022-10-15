using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.SyntaxTree;

namespace Trophy.Parsing.ParseTree
{
    public interface IParseTree {
        public TokenLocation Location { get; }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context);

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types) {
            return this.ResolveTypes(scope, names, types, TypeContext.None);
        }
    }
}
