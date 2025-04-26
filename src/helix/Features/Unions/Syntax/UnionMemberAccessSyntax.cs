using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions.Syntax {
    public record UnionMemberAccessSyntax : ISyntax {
        public required ISyntax Operand { get; init; }

        public required string MemberName { get; init; }

        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }

        public bool AlwaysJumps => Operand.AlwaysJumps;
        
        public virtual ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CMemberAccess {
                MemberName = this.MemberName,
                IsPointerAccess = false,
                Target = new CMemberAccess {
                    MemberName = "data",
                    IsPointerAccess = false,
                    Target = this.Operand.GenerateCode(types, writer)
                },
            };
        }
    }
}
