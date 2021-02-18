using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;
using Attempt20.Features.Arrays;
using Attempt20.Features.FlowControl;

namespace Attempt20.Features.Containers {
    public class VoidToArrayAdapter : ITypeCheckedSyntax {
        public TokenLocation Location => this.Target.Location;

        public LanguageType ReturnType { get; set; }

        public ITypeCheckedSyntax Target { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var syntax = new BlockTypeCheckedSyntax() {
                Lifetimes = this.Target.Lifetimes,
                Location = this.Location,
                ReturnType = this.ReturnType,
                Statements = new[] {
                    this.Target,
                    new ArrayTypeCheckedLiteral() {
                        Arguments = new ITypeCheckedSyntax[0],
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
