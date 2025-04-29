using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives;

public class TypeAdapterTree : ITypedTree {
    public required ITypedTree Operand { get; init; }
    
    public required HelixType ReturnType { get; init; }
    
    public TokenLocation Location => this.Operand.Location;

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        return this.Operand.GenerateIR(writer, context);
    }
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return this.Operand.GenerateCode(types, writer);
    }
}