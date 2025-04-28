using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax;

public interface ILValue {
    public HelixType ReturnType { get; }
    
    public record Local(IdentifierPath VariablePath, HelixType ReturnType) : ILValue {
    }
    
    public record Dereference(ITypedTree Operand) : ILValue {
        public HelixType ReturnType => this.Operand.ReturnType;
    }

    public record ArrayIndex(ITypedTree Operand, ITypedTree Index, HelixType ReturnType) : ILValue {
    }

    public record StructMemberAccess(ILValue Parent, string MemberName, HelixType ReturnType) : ILValue {
    }
}