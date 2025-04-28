using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.FlowControl {
    public record LoopControlTree : IParseTree, ITypedTree {
        public required LoopControlKind Kind { get; init; }

        public required TokenLocation Location { get; init; }

        public bool AlwaysJumps => true;

        public HelixType ReturnType => PrimitiveType.Void;

        public bool IsPure => false;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            if (this.Kind == LoopControlKind.Break) {
                types = types.WithBreakFrame(types);
            }
            else {
                types = types.WithContinueFrame(types);
            }

            return new TypeCheckResult(this, types);
        }

        public Immediate GenerateIR(IRWriter writer, IRFrame context) {
            if (this.Kind == LoopControlKind.Break) {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = context.BreakBlock!
                });
            }
            else {
                writer.CurrentBlock.Terminate(new JumpOp {
                    BlockName = context.ContinueBlock!
                });
            }

            return new Immediate.Void();
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (this.Kind == LoopControlKind.Break) {
                writer.WriteStatement(new CBreak());
            }
            else {
                writer.WriteStatement(new CContinue());
            }

            return new CIntLiteral(0);
        }
    }
}
