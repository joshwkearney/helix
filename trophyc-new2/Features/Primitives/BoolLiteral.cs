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
        private IParseTree BoolLiteral() {
            var start = this.Advance(TokenKind.BoolLiteral);
            var value = bool.Parse(start.Value);

            return new BoolParseLiteral(start.Location, value);
        }
    }
}

namespace Trophy.Features.Primitives {
    public class BoolParseLiteral : IParseTree {
        public TokenLocation Location { get; }

        public bool Value { get; }

        public TrophyType ReturnType => PrimitiveType.Bool;

        public BoolParseLiteral(TokenLocation loc, bool value) {
            this.Location = loc;
            this.Value = value;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            return new BoolLiteral(this.Value);
        }
    }

    public class BoolLiteral : ISyntaxTree {
        public bool Value { get; }

        public TrophyType ReturnType => PrimitiveType.Bool;

        public BoolLiteral(bool value) {
            this.Value = value;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            return CExpression.IntLiteral(this.Value ? 1 : 0);
        }
    }
}
