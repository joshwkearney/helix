using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives;

public record TypedBinaryExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }

    public required ITypedExpression Left { get; init; }
        
    public required ITypedExpression Right { get; init; }
        
    public BinaryOperationKind Operator { get; init; }

    public required HelixType ReturnType { get; init; }
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CBinaryExpression {
            Left = this.Left.GenerateCode(types, writer),
            Right = this.Right.GenerateCode(types, writer),
            Operation = this.Operator
        };
    }

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var left = this.Left.GenerateIR(writer, context);
        var right = this.Right.GenerateIR(writer, context);
        var name = writer.GetName();

        writer.CurrentBlock.Add(new BinaryInstruction {
            Left = left,
            Right = right,
            Operation = this.Operator,
            ReturnType = this.ReturnType,
            ReturnValue = name
        });

        return name;
    }
}