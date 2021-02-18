using System;
using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Functions {
    public class FunctionAccessParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IdentifierPath FunctionPath { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            throw new InvalidOperationException();
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            return new FunctionAccessTypeCheckedSyntax() {
                Location = this.Location,
                ReturnType = new SingularFunctionType(this.FunctionPath)
            };
        }
    }

    public class FunctionAccessTypeCheckedSyntax : ISyntax {
        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}