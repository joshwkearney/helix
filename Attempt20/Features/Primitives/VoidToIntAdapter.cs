using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Primitives {
    public class VoidToPrimitiveAdapter : ITypeCheckedSyntax {
        public TokenLocation Location => this.Target.Location;

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public ITypeCheckedSyntax Target { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            this.Target.GenerateCode(declWriter, statWriter);

            return CExpression.IntLiteral(0);
        }
    }

    public class BoolToIntAdapter : ITypeCheckedSyntax {
        public TokenLocation Location => this.Target.Location;

        public LanguageType ReturnType => LanguageType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public ITypeCheckedSyntax Target { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
