using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Features.FlowControl;

namespace Trophy.Features.Containers.Arrays {
    public class VoidToArrayAdapterC : ISyntaxC {
        public readonly ISyntaxC target;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public VoidToArrayAdapterC(ISyntaxC target, ITrophyType returnType) {
            this.target = target;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            // Note: specifying a particular stack here is ok because void arrays are value types.
            var syntax = new BlockSyntaxC(new[] {
                this.target,
                new ArrayLiteralSyntaxC(new IdentifierPath("heap", "stack"), new ISyntaxC[0], this.ReturnType)
            });

            return syntax.GenerateCode(declWriter, statWriter);
        }
    }
}
