namespace Helix.MiddleEnd.TypeChecking {
    public enum ControlFlowCertainty {
        Yes, No, Maybe
    }

    public record struct ControlFlow(ControlFlowCertainty FunctionReturn, ControlFlowCertainty Continue, ControlFlowCertainty Break) {
        public static ControlFlow NormalFlow { get; } = new ControlFlow(ControlFlowCertainty.No, ControlFlowCertainty.No, ControlFlowCertainty.No);

        public static ControlFlow BreakFlow { get; } = new ControlFlow(ControlFlowCertainty.No, ControlFlowCertainty.No, ControlFlowCertainty.Yes);

        public static ControlFlow ContinueFlow { get; } = new ControlFlow(ControlFlowCertainty.No, ControlFlowCertainty.Yes, ControlFlowCertainty.No);

        public static ControlFlow FunctionReturnFlow { get; } = new ControlFlow(ControlFlowCertainty.Yes, ControlFlowCertainty.No, ControlFlowCertainty.No);

        public bool DoesFunctionReturn => this.FunctionReturn == ControlFlowCertainty.Yes;

        public bool DoesBreak => this.Break == ControlFlowCertainty.Yes;

        public bool DoesContinue => this.Continue == ControlFlowCertainty.Yes;

        public bool DoesLoopReturn => this.Continue == ControlFlowCertainty.Yes || this.Break == ControlFlowCertainty.Yes;

        public bool CouldFunctionReturn => this.FunctionReturn == ControlFlowCertainty.Yes || this.FunctionReturn == ControlFlowCertainty.Maybe;

        public bool CouldLoopReturn => this.DoesLoopReturn
            || this.Break == ControlFlowCertainty.Maybe 
            || this.Continue == ControlFlowCertainty.Maybe;

        public bool CouldJump => this.CouldFunctionReturn || this.CouldLoopReturn;

        public bool DoesJump => this.FunctionReturn == ControlFlowCertainty.Yes || this.DoesLoopReturn;

        public ControlFlow Merge(ControlFlow other) {
            var funcReturn = ControlFlowCertainty.No;
            var breakFlow = ControlFlowCertainty.No;
            var continueFlow = ControlFlowCertainty.No;

            if (this.FunctionReturn == ControlFlowCertainty.Yes && other.FunctionReturn == ControlFlowCertainty.Yes) {
                funcReturn = ControlFlowCertainty.Yes;
            }
            else if (this.FunctionReturn == ControlFlowCertainty.Yes || other.FunctionReturn == ControlFlowCertainty.Yes) {
                funcReturn = ControlFlowCertainty.Maybe;
            }
            else if (this.FunctionReturn == ControlFlowCertainty.Maybe || other.FunctionReturn == ControlFlowCertainty.Maybe) {
                funcReturn = ControlFlowCertainty.Maybe;
            }

            if (this.Continue == ControlFlowCertainty.Yes && other.Continue == ControlFlowCertainty.Yes) {
                continueFlow = ControlFlowCertainty.Yes;
            }
            else if (this.Continue == ControlFlowCertainty.Yes || other.Continue == ControlFlowCertainty.Yes) {
                continueFlow = ControlFlowCertainty.Maybe;
            }
            else if (this.Continue == ControlFlowCertainty.Maybe || other.Continue == ControlFlowCertainty.Maybe) {
                continueFlow = ControlFlowCertainty.Maybe;
            }

            if (this.Break == ControlFlowCertainty.Yes && other.Break == ControlFlowCertainty.Yes) {
                breakFlow = ControlFlowCertainty.Yes;
            }
            else if (this.Break == ControlFlowCertainty.Yes || other.Break == ControlFlowCertainty.Yes) {
                breakFlow = ControlFlowCertainty.Maybe;
            }
            else if (this.Break == ControlFlowCertainty.Maybe || other.Break == ControlFlowCertainty.Maybe) {
                breakFlow = ControlFlowCertainty.Maybe;
            }

            return new ControlFlow(funcReturn, continueFlow, breakFlow);
        }
    }
}
