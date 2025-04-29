using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree;

public interface ILValue {
    public HelixType ReturnType { get; }
    
    public record Local(IdentifierPath VariablePath, HelixType ReturnType) : ILValue {
    }
    
    public record Dereference(ITypedExpression Operand) : ILValue {
        public HelixType ReturnType => this.Operand.ReturnType;
    }

    public record ArrayIndex(ITypedExpression Operand, ITypedExpression Index, HelixType ReturnType) : ILValue {
    }

    public record StructMemberAccess(ILValue Parent, string MemberName, HelixType ReturnType) : ILValue {
    }
}