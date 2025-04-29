using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;

namespace Helix.Syntax.TypedTree.FlowControl {
    public record TypedIfStatement : ITypedStatement {
        public required TokenLocation Location { get; init; }

        public required ITypedExpression Condition { get; init; }
        
        public required ITypedStatement Affirmative { get; init; }
        
        public required ITypedStatement  Negative { get; init; }
        
        public required bool AlwaysJumps { get; init; }

        public void GenerateIR(IRWriter writer, IRFrame context) {
            var trueBranchName = writer.GetBlockName("if_true");
            var falseBranchName = writer.GetBlockName("if_false");
            var continueBranchName = writer.GetBlockName("if_after");
            var cond = this.Condition.GenerateIR(writer, context);
            
            writer.CurrentBlock.Terminate(new JumpConditionalOp {
                Condition = cond,
                TrueBlockName = trueBranchName,
                FalseBlockName = falseBranchName
            });
            
            writer.PopBlock();
            writer.PushBlock(trueBranchName);

            this.Affirmative.GenerateIR(writer, context);
            
            if (!writer.CurrentBlock.IsTerminated) {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = continueBranchName
                });
            }

            writer.PopBlock();
            writer.PushBlock(falseBranchName);
            
            this.Negative.GenerateIR(writer, context);

            if (!writer.CurrentBlock.IsTerminated) {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = continueBranchName
                });
            }

            writer.PopBlock();
            writer.PushBlock(continueBranchName);
        }

        public void GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            this.Affirmative.GenerateCode(types, affirmWriter);
            this.Negative.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();

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
            writer.WriteStatement(expr);
            writer.WriteEmptyLine();
        }
    }
}