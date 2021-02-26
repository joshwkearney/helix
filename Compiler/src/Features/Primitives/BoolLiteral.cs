using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public class BoolLiteralSyntax : ISyntaxA, ISyntaxB, ISyntaxC {
        private readonly bool value;

        public TokenLocation Location { get; }

        public TrophyType ReturnType => TrophyType.Boolean;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>();
        }

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