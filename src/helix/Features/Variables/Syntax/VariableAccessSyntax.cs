using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables;

public record VariableAccessSyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public required PointerType VariableSignature { get; init; }

    public required IdentifierPath VariablePath { get; init; }
        
    public required bool IsLValue { get; init; }

    public HelixType ReturnType {
        get {
            if (this.IsLValue) {
                return new NominalType(this.VariablePath, NominalTypeKind.Variable);
            }
            else {
                return this.VariableSignature.InnerType;
            }
        }
    }

    public ISyntax ToLValue(TypeFrame types) {
        return this with {
            IsLValue = true
        };
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        ICSyntax result = new CVariableLiteral(writer.GetVariableName(this.VariablePath));

        if (writer.VariableKinds[this.VariablePath] == CVariableKind.Allocated) {
            result = new CPointerDereference() {
                Target = result
            };
        }

        if (this.IsLValue) {
            result = new CAddressOf() {
                Target = result
            };
        }

        return result;
    }
}