using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables.Syntax;

public record VariableAccessSyntax : ISyntax {
    public required TokenLocation Location { get; init; }
    
    public required IdentifierPath VariablePath { get; init; }
    
    public required HelixType ReturnType { get; init; }
    
    public bool AlwaysJumps => false;
    
    public ILValue ToLValue(TypeFrame types) {
        return new ILValue.Local(this.VariablePath, types.Signatures[this.VariablePath]);
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        ICSyntax result = new CVariableLiteral(writer.GetVariableName(this.VariablePath));

        if (writer.VariableKinds[this.VariablePath] == CVariableKind.Allocated) {
            result = new CPointerDereference {
                Target = result
            };
        }

        return result;
    }
}