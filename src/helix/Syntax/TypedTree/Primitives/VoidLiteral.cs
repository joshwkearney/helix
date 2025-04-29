using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.ParseTree;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives;

public record VoidLiteral : IParseExpression, ITypedExpression {
    public required TokenLocation Location { get; init; }

    public bool AlwaysJumps => false;

    public HelixType ReturnType => PrimitiveType.Void;

    public bool IsPure => true;

    public Option<HelixType> AsType(TypeFrame types) => PrimitiveType.Void;

    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) => new(this, types);

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CIntLiteral(0);
    }

    public Immediate GenerateIR(IRWriter writer, IRFrame context) => new Immediate.Void();
}