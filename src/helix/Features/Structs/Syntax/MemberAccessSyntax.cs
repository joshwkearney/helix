using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs.Syntax {
    public record MemberAccessSyntax : ISyntax {
        public required ISyntax Operand { get; init; }

        public required string MemberName { get; init; }

        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }

        public bool AlwaysJumps => this.Operand.AlwaysJumps;

        public ILValue ToLValue(TypeFrame types) {
            var target = this.Operand.ToLValue(types);

            return new ILValue.StructMemberAccess(target, this.MemberName, new PointerType(this.ReturnType));
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
