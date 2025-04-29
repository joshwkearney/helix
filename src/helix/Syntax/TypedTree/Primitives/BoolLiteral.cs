using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.ParseTree;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives;

public record BoolLiteral : IParseExpression, ITypedExpression {
    public required TokenLocation Location { get; init; }
        
    public required bool Value { get; init; }

    public bool AlwaysJumps => false;

    public HelixType ReturnType => new SingularBoolType(this.Value);
        
    public bool IsPure => true;
        
    public Option<HelixType> AsType(TypeFrame types) {
        return new SingularBoolType(this.Value);
    }

    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) => new(this, types);

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CIntLiteral(this.Value ? 1 : 0);
    }

    public Immediate GenerateIR(IRWriter writer, IRFrame context) => new Immediate.Bool(this.Value);
}