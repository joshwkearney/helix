using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Variables;

public class TypedAddressOfExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }

    public required HelixType ReturnType { get; init; }
        
    public required IdentifierPath VariablePath { get; init; }
        
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        // If we're taking the address of a local, it's already been promoted to heap allocated and
        // the variable is actually storing a reference. We can just return that
        return context.GetVariable(this.VariablePath);
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CCompoundExpression {
            Arguments = [
                new CVariableLiteral(writer.GetVariableName(this.VariablePath))
            ],
            Type = writer.ConvertType(this.ReturnType, types)
        };
    }
}