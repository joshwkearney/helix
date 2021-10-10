using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using System.Collections.Immutable;

namespace Trophy.Features.Variables {
    public class VarToRefAdapter : ISyntaxC {
        private readonly ISyntaxC target;

        public ITrophyType ReturnType { get; }

        public VarToRefAdapter(ISyntaxC target, ITrophyType returnType) {
            this.target = target;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            return this.target.GenerateCode(writer, statWriter);
        }
    }
}
