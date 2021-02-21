using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class BoolLiteralSyntax : ISyntaxA, ISyntaxB, ISyntaxC {
        private readonly bool value;

        public TokenLocation Location { get; }

        public TrophyType ReturnType => TrophyType.Boolean;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public BoolLiteralSyntax(TokenLocation loc, bool value) {
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
            return this.value ? CExpression.IntLiteral(1) : CExpression.IntLiteral(0);
        }

        public override string ToString() {
            return this.value.ToString().ToLower();
        }
    }
}