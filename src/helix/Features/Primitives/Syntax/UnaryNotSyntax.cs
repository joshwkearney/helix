using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives.IR;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.Syntax;

public record UnaryNotSyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public required HelixType ReturnType { get; init; }
    
    public required ISyntax Operand { get; init; }
    
    public required bool AlwaysJumps { get; init; }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CNot() {
            Target = this.Operand.GenerateCode(types, writer)
        };
    }

    public Immediate GenerateIR(IRWriter writer, IRFrame context, Immediate? returnName = null) {
        var operand = this.Operand.GenerateIR(writer, context);
        var name = returnName ?? writer.GetVariable();
        
        writer.WriteOp(new UnaryOp {
            Operation = UnaryOperatorKind.Not,
            Operand = operand,
            ReturnValue = name,
            ReturnType = this.ReturnType
        });

        return name;
    }
}