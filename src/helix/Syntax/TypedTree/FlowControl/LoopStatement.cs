using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.FlowControl {
    public record LoopStatement : ITypedStatement {
        public required TokenLocation Location { get; init; }

        public required ITypedStatement Body { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;
        
        public void GenerateIR(IRWriter writer, IRFrame context) {
            var loopBlock = writer.GetBlockName("loop");
            var afterBlock = writer.GetBlockName("loop_after");
            
            writer.CurrentBlock.Terminate(new JumpOp {
                BlockName = loopBlock
            });
            
            writer.PopBlock();
            writer.PushBlock(loopBlock);
            context.PushLoop(afterBlock, loopBlock);
            
            this.Body.GenerateIR(writer, context);

            if (!writer.CurrentBlock.IsTerminated) {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = loopBlock
                });
            }

            writer.PopBlock();
            writer.PushBlock(afterBlock);
            context.PopLoop();
        }

        public void GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var bodyStats = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, bodyStats);

            this.Body.GenerateCode(types, bodyWriter);

            if (bodyStats.Any() && bodyStats.Last().IsEmpty) {
                bodyStats.RemoveAt(bodyStats.Count - 1);
            }

            var stat = new CWhile {
                Condition = new CIntLiteral(1),
                Body = bodyStats
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Loop");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();
        }
    }
}
