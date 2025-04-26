using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions.Syntax {
    public record FunctionAccessSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required IdentifierPath FunctionPath { get; init; }
        
        public bool AlwaysJumps => false;

        public HelixType ReturnType => new NominalType(this.FunctionPath, NominalTypeKind.Function);

        public bool IsPure => true;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CVariableLiteral(writer.GetVariableName(this.FunctionPath));
        }
    }
}