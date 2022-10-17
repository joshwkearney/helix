using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;

namespace Trophy.Features.Meta {
    public class TypeSyntaxC : ISyntaxC {
        public ITrophyType ReturnType { get; }

        public TypeSyntaxC(ITrophyType type) {
            this.ReturnType = type;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}