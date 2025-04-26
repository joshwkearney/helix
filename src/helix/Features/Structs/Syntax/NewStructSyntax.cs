using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs.Syntax {
    public class NewStructSyntax : ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required StructType Signature { get; init; }
        
        public required IReadOnlyList<string> Names { get; init; }
        
        public required IReadOnlyList<ISyntax> Values { get; init; }
        
        public required bool AlwaysJumps { get; init; }

        public HelixType ReturnType => this.Signature;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var memValues = this.Values
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            if (memValues.Length == 0) {
                memValues = [new CIntLiteral(0)];
            }

            return new CCompoundExpression() {
                Type = writer.ConvertType(this.Signature, types),
                MemberNames = this.Names,
                Arguments = memValues,
            };
        }
    }
}