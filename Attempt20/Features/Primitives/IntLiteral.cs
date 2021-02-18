using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Primitives {
    public class IntLiteralSyntax : IParsedSyntax, ITypeCheckedSyntax {
        public int Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType => LanguageType.Integer;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public IParsedSyntax CheckNames(INameRecorder names) {
            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            return this;
        }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(this.Value);
        }

        public override string ToString() {
            return this.Value.ToString();
        }
    }
}