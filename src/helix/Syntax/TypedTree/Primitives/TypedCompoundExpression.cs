using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives;

public record TypedCompoundExpression : ITypedExpression {
    public required ITypedExpression First { get; init; }
    
    public required ITypedExpression Second { get; init; }

    public TokenLocation Location => this.First.Location.Span(this.Second.Location);

    public HelixType ReturnType => this.Second.ReturnType;
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        this.First.GenerateCode(types, writer);

        return this.Second.GenerateCode(types, writer);
    }
    
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        this.First.GenerateIR(writer, context);

        return this.Second.GenerateIR(writer, context);
    }
}