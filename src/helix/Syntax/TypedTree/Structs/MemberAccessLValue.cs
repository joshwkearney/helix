using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Structs;

public record MemberAccessLValue : ITypedTree {
    public required TokenLocation Location { get; init; }
    
    public required ITypedTree Operand { get; init; }
    
    public required string MemberName { get; init; }
    
    public required HelixType ReturnType { get; init; }
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        // Our target will be converted to an lvalue, so we have to dereference it first
        var target = new CPointerDereference {
            Target = this.Operand.GenerateCode(types, writer)
        };

        return new CAddressOf {
            Target = new CMemberAccess {
                Target = target,
                MemberName = this.MemberName,
                IsPointerAccess = false
            }
        };
    }
}