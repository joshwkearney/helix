using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class IntLiteralSyntax : ISyntaxA, ISyntaxB, ISyntaxC {
        private readonly int value;

        public TokenLocation Location { get; }

        public TrophyType ReturnType => TrophyType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public IntLiteralSyntax(TokenLocation loc, int value) {
            this.Location = loc;
            this.value = value;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            return this;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            return this;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(this.value);
        }

        public override string ToString() {
            return this.value.ToString();
        }
    }
}