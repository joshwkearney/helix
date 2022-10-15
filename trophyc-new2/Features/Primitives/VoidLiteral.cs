using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseTree VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral(tok.Location);
        }
    }
}

namespace Trophy.Features.Primitives {
    public class VoidLiteral : IParseTree, ISyntaxTree {
        public TokenLocation Location { get; }

        public TrophyType ReturnType => PrimitiveType.Void;

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            return this;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}
