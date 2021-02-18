using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Containers {
    public class FixedArrayToArrayAdapter : ITypeCheckedSyntax {
        public ITypeCheckedSyntax Target { get; set; }

        public TokenLocation Location => this.Target.Location;

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
