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
        private IParseTree IntLiteral() {
            var tok = this.Advance(TokenKind.IntLiteral);
            var num = int.Parse(tok.Value);

            return new IntParseLiteral(tok.Location, num);
        }
    }
}

namespace Trophy.Features.Primitives {
    public class IntParseLiteral : IParseTree {
        public TokenLocation Location { get; }

        public int Value { get; }

        public IntParseLiteral(TokenLocation loc, int value) {
            this.Location = loc;
            this.Value = value;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            return new IntLiteral(this.Value);
        }
    }

    public class IntLiteral : ISyntaxTree {
        public int Value { get; }

        public TrophyType ReturnType => PrimitiveType.Int;

        public IntLiteral(int value) {
            this.Value = value;
        }


        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            return CExpression.IntLiteral(this.Value);
        }
    }
}
