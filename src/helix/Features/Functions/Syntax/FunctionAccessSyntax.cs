using Helix.Analysis.TypeChecking;
using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Features.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions {
    public record FunctionAccessSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required IdentifierPath FunctionPath { get; init; }
        
        public required FunctionType FunctionSignature { get; init; }

        public HelixType ReturnType => this.FunctionSignature;

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;
        
        public bool IsPure => true;

        public ISyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CVariableLiteral(writer.GetVariableName(this.FunctionPath));
        }
    }
}