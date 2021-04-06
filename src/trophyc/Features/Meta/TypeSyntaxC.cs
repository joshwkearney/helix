using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Features.Meta {
    public class TypeSyntaxC : ISyntaxC {
        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public TypeSyntaxC(ITrophyType type, ImmutableHashSet<IdentifierPath> lifetimes) {
            this.ReturnType = type;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}