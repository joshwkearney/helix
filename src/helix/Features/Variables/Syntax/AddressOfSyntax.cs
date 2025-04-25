using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;

namespace Helix.Features.Variables {
    public class AddressOfSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }
        
        public required ISyntax Operand { get; init; }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CCompoundExpression {
                Arguments = [
                    this.Operand.GenerateCode(types, writer)
                ],
                Type = writer.ConvertType(this.ReturnType, types)
            };
        }
    }
}