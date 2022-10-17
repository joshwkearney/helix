using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public class BoolLiteralSyntax : ISyntaxA, ISyntaxB, ISyntaxC {
        private readonly bool value;

        public TokenLocation Location { get; }

        public ITrophyType ReturnType => ITrophyType.Boolean;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public IImmutableSet<VariableUsage> VariableUsage {
            get => ImmutableHashSet.Create<VariableUsage>();
        }

        public BoolLiteralSyntax(TokenLocation loc, bool value) {
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
            return this.value ? CExpression.IntLiteral(1) : CExpression.IntLiteral(0);
        }

        public override string ToString() {
            return this.value.ToString().ToLower();
        }
    }
}