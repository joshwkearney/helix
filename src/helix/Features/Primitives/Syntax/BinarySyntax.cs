using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives.IR;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.Syntax;

public record BinarySyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public ISyntax Left { get; init; }
        
    public ISyntax Right { get; init; }
        
    public BinaryOperationKind Operator { get; init; }

    public required HelixType ReturnType { get; init; }
    
    public required bool AlwaysJumps { get; init; }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CBinaryExpression() {
            Left = this.Left.GenerateCode(types, writer),
            Right = this.Right.GenerateCode(types, writer),
            Operation = this.Operator
        };
    }

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var left = this.Left.GenerateIR(writer, context);
        var right = this.Right.GenerateIR(writer, context);
        var name = writer.GetName();

        writer.WriteOp(new BinaryOp {
            Left = left,
            Right = right,
            Operation = this.Operator,
            ReturnType = this.ReturnType,
            ReturnValue = name
        });

        return name;
    }
}