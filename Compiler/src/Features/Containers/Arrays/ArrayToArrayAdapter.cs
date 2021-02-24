using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;

namespace Attempt20.Features.Containers.Arrays {
    public class ArrayToArrayAdapter : ISyntaxC {
        private readonly ISyntaxC target;

        public TrophyType ReturnType { get; }

        public ArrayToArrayAdapter(ISyntaxC target, TrophyType returnType) {
            this.target = target;
            this.ReturnType = returnType;
        }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.target.GenerateCode(declWriter, statWriter);
        }
    }
}
