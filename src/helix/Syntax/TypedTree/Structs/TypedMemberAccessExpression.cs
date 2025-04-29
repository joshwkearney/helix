using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Structs;

public record TypedMemberAccessExpression : ITypedExpression {
    public required ITypedExpression Operand { get; init; }

    public required string MemberName { get; init; }

    public required TokenLocation Location { get; init; }

    public required HelixType ReturnType { get; init; }
        
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