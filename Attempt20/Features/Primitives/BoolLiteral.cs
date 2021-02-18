using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Primitives {
    public class BoolLiteralSyntax : IParsedSyntax, ITypeCheckedSyntax {
        public bool Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType => LanguageType.Boolean;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public IParsedSyntax CheckNames(INameRecorder names) {
            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            return this;
        }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            return this.Value ? CExpression.IntLiteral(1) : CExpression.IntLiteral(0);
        }

        public override string ToString() {
            return this.Value.ToString().ToLower();
        }
    }
}