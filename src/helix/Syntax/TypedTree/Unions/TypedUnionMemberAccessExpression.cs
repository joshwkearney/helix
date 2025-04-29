using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Unions;

public record TypedUnionMemberAccessExpression : ITypedExpression {
    public required ITypedExpression Operand { get; init; }

    public required string MemberName { get; init; }

    public required TokenLocation Location { get; init; }

    public required HelixType ReturnType { get; init; }
        
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