using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class IntLiteralSyntax : IParsedSyntax, ISyntax {
        public int Value { get; set; }

        public TokenLocation Location { get; set; }

        public TrophyType ReturnType => TrophyType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public IParsedSyntax CheckNames(INameRecorder names) {
            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            return this;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(this.Value);
        }

        public override string ToString() {
            return this.Value.ToString();
        }
    }
}