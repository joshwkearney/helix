using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Collections.Immutable;

namespace Helix.MiddleEnd.TypeChecking {
    internal class ControlFlowFrame {
        public IHelixType FunctionReturnType { get; private set; }

        public bool IsInsideLoop { get; private set; }

        /// <summary>
        /// A set of aliases that exist after the current loop exits. This
        /// will always be because of a break statement since all loops are
        /// infinite by default in Hmm
        /// </summary>
        public ImmutableHashSet<AnalysisScope> LoopAppendix { get; private set; }

        public ControlFlowFrame() {
            this.IsInsideLoop = false;
            this.FunctionReturnType = VoidType.Instance;
            this.LoopAppendix = [];
        }

        private ControlFlowFrame(IHelixType returnType) {
            this.IsInsideLoop = true;
            this.FunctionReturnType = returnType;
            this.LoopAppendix = [];
        }

        public ControlFlowFrame CreateLoopFrame() {
            return new ControlFlowFrame(FunctionReturnType);
        }

        public ControlFlowFrame CreateFunctionFrame(IHelixType returnType) {
            return new ControlFlowFrame(returnType);
        }

        public void AddLoopAppendix(AnalysisScope scope) {
            Assert.IsTrue(IsInsideLoop);

            this.LoopAppendix = this.LoopAppendix.Add(scope);
        }

        public void SetReturnType(IHelixType returnType) {
            this.FunctionReturnType = returnType;
        }
    }
}
