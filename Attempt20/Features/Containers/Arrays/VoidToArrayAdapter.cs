using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.FlowControl;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers.Arrays {
    public class VoidToArrayAdapter : ISyntax {
        public TokenLocation Location => this.Target.Location;

        public TrophyType ReturnType { get; set; }

        public ISyntax Target { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var syntax = new BlockTypeCheckedSyntax() {
                Lifetimes = this.Target.Lifetimes,
                Location = this.Location,
                ReturnType = this.ReturnType,
                Statements = new[] {
                    this.Target,
                    new ArrayTypeCheckedLiteral() {
                        Arguments = new ISyntax[0],
                        Lifetimes = this.Target.Lifetimes,
                        Location = this.Target.Location,
                        RegionName = "stack",
                        ReturnType = this.ReturnType
                    }
                }
            };

            return syntax.GenerateCode(declWriter, statWriter);
        }
    }
}
