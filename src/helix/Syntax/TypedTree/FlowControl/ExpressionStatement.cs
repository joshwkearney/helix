using Helix.CodeGeneration;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;

namespace Helix.Syntax.TypedTree.FlowControl;

public class ExpressionStatement : ITypedStatement {
    public required ITypedTree Expression { get; init; }

    public TokenLocation Location => this.Expression.Location;
    
    public bool AlwaysJumps => false;
    
    public void GenerateCode(TypeFrame types, ICStatementWriter writer) {
        this.Expression.GenerateCode(types, writer);
    }
    
    public void GenerateIR(IRWriter writer, IRFrame context) {
        this.Expression.GenerateIR(writer, context);
    }
}