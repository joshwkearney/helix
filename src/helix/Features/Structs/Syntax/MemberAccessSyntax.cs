using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Structs {
    public record MemberAccessSyntax : ISyntax {
        public required ISyntax Operand { get; init; }

        public required string MemberName { get; init; }

        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }

        public ISyntax ToLValue(TypeFrame types) {
            var target = this.Operand.ToLValue(types);

            var result = new MemberAccessLValue {
                Location = this.Location,
                Operand = target,
                MemberName = this.MemberName,
                ReturnType = this.ReturnType
            };

            return result;
        }
        
        public virtual ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CMemberAccess {
                Target = this.Operand.GenerateCode(types, writer),
                MemberName = this.MemberName,
                IsPointerAccess = false
            };
        }
    }
}
