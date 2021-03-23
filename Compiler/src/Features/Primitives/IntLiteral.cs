using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public class IntLiteralSyntax : ISyntaxA, ISyntaxB, ISyntaxC {
        private readonly int value;

        public TokenLocation Location { get; }

        public ITrophyType ReturnType => ITrophyType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>();
        }

        public IntLiteralSyntax(TokenLocation loc, int value) {
            this.Location = loc;
            this.value = value;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            return this;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
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