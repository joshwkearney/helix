using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;

namespace Trophy.Features.Containers.Arrays {
    public class ArrayToArrayAdapter : ISyntaxC {
        private readonly ISyntaxC target;

        public ITrophyType ReturnType { get; }

        public ArrayToArrayAdapter(ISyntaxC target, ITrophyType returnType) {
            this.target = target;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.target.GenerateCode(declWriter, statWriter);
        }
    }
}
