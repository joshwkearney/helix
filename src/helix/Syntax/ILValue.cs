using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Syntax;

public interface ILValue {
    public HelixType ReturnType { get; }
    
    public record Local(IdentifierPath VariablePath, HelixType ReturnType) : ILValue {
    }
    
    public record Dereference(ISyntax Operand) : ILValue {
        public HelixType ReturnType => this.Operand.ReturnType;
    }

    public record ArrayIndex(ISyntax Operand, ISyntax Index, HelixType ReturnType) : ILValue {
    }

    public record StructMemberAccess(ILValue Parent, string MemberName, HelixType ReturnType) : ILValue {
    }
}