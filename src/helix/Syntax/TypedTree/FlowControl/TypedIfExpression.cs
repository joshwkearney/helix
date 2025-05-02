using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.FlowControl;

public record TypedIfExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }

    public required ITypedExpression Condition { get; init; }
        
    public required ITypedExpression Affirmative { get; init; }
        
    public required ITypedExpression  Negative { get; init; }
        
    public required HelixType ReturnType { get; init; }
        
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var trueBranchName = writer.GetBlockName("if_true");
        var falseBranchName = writer.GetBlockName("if_false");
        var continueBranchName = writer.GetBlockName("if_after");
        var cond = this.Condition.GenerateIR(writer, context);
        var resultName = writer.GetName();

        // We need a result variable
        writer.CurrentBlock.Add(new CreateLocalInstruction {
            LocalName = resultName, 
            ReturnType = this.ReturnType
        });
            
        // Write out our conditional jump
        writer.CurrentBlock.Terminate(new JumpConditionalInstruction {
            Condition = cond,
            TrueBlockName = trueBranchName,
            FalseBlockName = falseBranchName
        });
            
        writer.PopBlock();
        writer.PushBlock(trueBranchName);

        var affirm = this.Affirmative.GenerateIR(writer, context);
            
        writer.CurrentBlock.Add(new AssignLocalInstruction {
            LocalName = resultName,
            Value = affirm
        });
            
        // If expressions won't terminate
        writer.CurrentBlock.Terminate(new JumpInstruction {
            BlockName = continueBranchName
        });

        writer.PopBlock();
        writer.PushBlock(falseBranchName);
            
        var neg = this.Negative.GenerateIR(writer, context);

        writer.CurrentBlock.Add(new AssignLocalInstruction {
            LocalName = resultName,
            Value = neg
        });

        // If expressions won't terminate
        writer.CurrentBlock.Terminate(new JumpInstruction {
            BlockName = continueBranchName
        });

        writer.PopBlock();
        writer.PushBlock(continueBranchName);

        return resultName;
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        var affirmList = new List<ICStatement>();
        var negList = new List<ICStatement>();

        var affirmWriter = new CStatementWriter(writer, affirmList);
        var negWriter = new CStatementWriter(writer, negList);

        var affirm = this.Affirmative.GenerateCode(types, affirmWriter);
        var neg = this.Negative.GenerateCode(types, negWriter);

        var tempName = writer.GetVariableName();

        affirmWriter.WriteStatement(new CAssignment {
            Left = new CVariableLiteral(tempName),
            Right = affirm
        });

        negWriter.WriteStatement(new CAssignment {
            Left = new CVariableLiteral(tempName),
            Right = neg
        });

        var tempStat = new CVariableDeclaration {
            Type = writer.ConvertType(this.ReturnType, types),
            Name = tempName
        };

        if (affirmList.Any() && affirmList.Last().IsEmpty) {
            affirmList.RemoveAt(affirmList.Count - 1);
        }

        if (negList.Any() && negList.Last().IsEmpty) {
            negList.RemoveAt(negList.Count - 1);
        }

        var expr = new CIf {
            Condition = this.Condition.GenerateCode(types, writer),
            IfTrue = affirmList,
            IfFalse = negList
        };

        writer.WriteEmptyLine();
        writer.WriteComment($"Line {this.Condition.Location.Line}: If statement");
        writer.WriteStatement(tempStat);
        writer.WriteStatement(expr);
        writer.WriteEmptyLine();

        return new CVariableLiteral(tempName);
    }
}