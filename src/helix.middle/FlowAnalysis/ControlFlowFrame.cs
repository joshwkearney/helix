using Helix.Common;
using Helix.Common.Collections;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.FlowAnalysis {
    internal class ControlFlowFrame {
        public IHelixType FunctionReturnType { get; private set; }

        public bool IsInsideLoop { get; private set; }

        /// <summary>
        /// A set of aliases that exist after the current loop exits. This
        /// will either be from a standard exit or a break statement.
        /// </summary>
        public ImmutableHashSet<AliasingTracker> LoopAppendixAliases { get; private set; }

        public ControlFlowFrame() {
            this.IsInsideLoop = false;
            this.FunctionReturnType = VoidType.Instance;
            this.LoopAppendixAliases = [];
        }

        private ControlFlowFrame(IHelixType returnType) {
            this.IsInsideLoop = true;
            this.FunctionReturnType = returnType;
            this.LoopAppendixAliases = [];
        }

        public ControlFlowFrame CreateLoopFrame() {
            return new ControlFlowFrame(this.FunctionReturnType);
        }

        public ControlFlowFrame CreateFunctionFrame(IHelixType returnType) {
            return new ControlFlowFrame(returnType);
        }

        public void AddLoopAppendix(AliasingTracker aliases) {
            Assert.IsTrue(this.IsInsideLoop);

            this.LoopAppendixAliases = this.LoopAppendixAliases.Add(aliases);
        }

        public void SetReturnType(IHelixType returnType) {
            this.FunctionReturnType = returnType;
        }
    }
}
