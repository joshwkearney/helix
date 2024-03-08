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
        /// will either be from a standard exit or a break statement.
        /// </summary>
        public ImmutableHashSet<AliasingTracker> LoopAppendixAliases { get; private set; }

        public ControlFlowFrame() {
            IsInsideLoop = false;
            FunctionReturnType = VoidType.Instance;
            LoopAppendixAliases = [];
        }

        private ControlFlowFrame(IHelixType returnType) {
            IsInsideLoop = true;
            FunctionReturnType = returnType;
            LoopAppendixAliases = [];
        }

        public ControlFlowFrame CreateLoopFrame() {
            return new ControlFlowFrame(FunctionReturnType);
        }

        public ControlFlowFrame CreateFunctionFrame(IHelixType returnType) {
            return new ControlFlowFrame(returnType);
        }

        public void AddLoopAppendix(AliasingTracker aliases) {
            Assert.IsTrue(IsInsideLoop);

            LoopAppendixAliases = LoopAppendixAliases.Add(aliases);
        }

        public void SetReturnType(IHelixType returnType) {
            FunctionReturnType = returnType;
        }
    }
}
