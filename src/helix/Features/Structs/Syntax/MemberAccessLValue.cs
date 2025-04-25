using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs;

public record MemberAccessLValue : ISyntax {
    public required TokenLocation Location { get; init; }
    
    public required ISyntax Operand { get; init; }
    
    public required string MemberName { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        // Our target will be converted to an lvalue, so we have to dereference it first
        var target = new CPointerDereference {
            Target = this.Operand.GenerateCode(types, writer)
        };

        return new CAddressOf() {
            Target = new CMemberAccess() {
                Target = target,
                MemberName = this.MemberName,
                IsPointerAccess = false
            }
        };
    }
}