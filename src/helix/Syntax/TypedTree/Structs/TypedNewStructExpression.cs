using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Structs;

public class TypedNewStructExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }
        
    public required StructSignature StructSignature { get; init; }
        
    public required HelixType StructType { get; init; }
        
    public required IReadOnlyList<string> Names { get; init; }
        
    public required IReadOnlyList<ITypedExpression> Values { get; init; }
        
    public HelixType ReturnType => this.StructType;

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var args = this.Values.Select(x => x.GenerateIR(writer, context)).ToArray();
        var structName = writer.GetName();

        writer.CurrentBlock.Add(new CreateLocalInstruction() {
            LocalName = structName,
            ReturnType = this.StructType
        });
        
        foreach (var (name, value) in this.Names.Zip(args)) {
            writer.CurrentBlock.Add(new AssignMemberOp() {
                LocalName = structName,
                MemberName = name,
                Value = value
            });
        }

        return structName;
    }
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        var memValues = this.Values
            .Select(x => x.GenerateCode(types, writer))
            .ToArray();

        if (memValues.Length == 0) {
            memValues = [new CIntLiteral(0)];
        }

        return new CCompoundExpression {
            Type = writer.ConvertType(this.StructType, types),
            MemberNames = this.Names,
            Arguments = memValues,
        };
    }
}