using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.FlowControl {
    public record IfTypedTree : ITypedTree {
        public required TokenLocation Location { get; init; }

        public required ITypedTree Condition { get; init; }
        
        public required ITypedTree Affirmative { get; init; }
        
        public required ITypedTree  Negative { get; init; }
        
        public required HelixType ReturnType { get; init; }
        
        public required bool AlwaysJumps { get; init; }

        public Immediate GenerateIR(IRWriter writer, IRFrame context) {
            var trueBranchName = writer.GetBlockName("if_true");
            var falseBranchName = writer.GetBlockName("if_false");
            var continueBranchName = writer.GetBlockName("if_after");
            var cond = this.Condition.GenerateIR(writer, context);
            var resultName = writer.GetName();

            // If we're returning an expression value with this if statement, we
            // need a result variable
            if (this.ReturnType != PrimitiveType.Void) {
                writer.CurrentBlock.Add(new CreateLocalOp {
                    LocalName = resultName, 
                    ReturnType = this.ReturnType
                });
            }
            
            // Write out our conditional jump
            writer.CurrentBlock.Terminate(new JumpConditionalOp {
                Condition = cond,
                TrueBlockName = trueBranchName,
                FalseBlockName = falseBranchName
            });
            
            writer.PopBlock();
            writer.PushBlock(trueBranchName);

            var affirm = this.Affirmative.GenerateIR(writer, context);

            if (this.ReturnType != PrimitiveType.Void) {
                writer.CurrentBlock.Add(new AssignLocalOp {
                    LocalName = resultName,
                    Value = affirm
                });
            }
            
            if (!writer.CurrentBlock.IsTerminated) {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = continueBranchName
                });
            }

            writer.PopBlock();
            writer.PushBlock(falseBranchName);
            
            var neg = this.Negative.GenerateIR(writer, context);

            if (this.ReturnType != PrimitiveType.Void) {
                writer.CurrentBlock.Add(new AssignLocalOp {
                    LocalName = resultName,
                    Value = neg
                });
            }

            if (!writer.CurrentBlock.IsTerminated) {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = continueBranchName
                });
            }

            writer.PopBlock();
            writer.PushBlock(continueBranchName);

            if (this.ReturnType == PrimitiveType.Void) {
                return new Immediate.Void();
            }
            else {
                return resultName;
            }
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.Affirmative.GenerateCode(types, affirmWriter);
            var neg = this.Negative.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();

            if (this.ReturnType != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var tempStat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.ReturnType, types),
                Name = tempName
            };

            if (affirmList.Any() && affirmList.Last().IsEmpty) {
                affirmList.RemoveAt(affirmList.Count - 1);
            }

            if (negList.Any() && negList.Last().IsEmpty) {
                negList.RemoveAt(negList.Count - 1);
            }

            var expr = new CIf() {
                Condition = this.Condition.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Condition.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (this.ReturnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            if (this.ReturnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}