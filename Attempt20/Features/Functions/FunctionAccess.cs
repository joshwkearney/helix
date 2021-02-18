using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Functions {
    public class FunctionAccessParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IdentifierPath FunctionPath { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            throw new InvalidOperationException();
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            return new FunctionAccessTypeCheckedSyntax() {
                Location = this.Location,
                ReturnType = new SingularFunctionType(this.FunctionPath)
            };
        }
    }

    public class FunctionAccessTypeCheckedSyntax : ITypeCheckedSyntax {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}