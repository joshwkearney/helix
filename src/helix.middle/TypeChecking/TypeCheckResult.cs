namespace Helix.MiddleEnd.TypeChecking {
    public record struct TypeCheckResult(string ResultName, ControlFlow ControlFlow) {
        public static TypeCheckResult VoidResult { get; } = new TypeCheckResult("void", ControlFlow.NormalFlow);

        public static TypeCheckResult Break { get; } = new TypeCheckResult("void", ControlFlow.BreakFlow);

        public static TypeCheckResult Continue { get; } = new TypeCheckResult("void", ControlFlow.ContinueFlow);

        public static TypeCheckResult FunctionReturn { get; } = new TypeCheckResult("void", ControlFlow.FunctionReturnFlow);

        public static TypeCheckResult NormalFlow(string resultName) => new TypeCheckResult(resultName, ControlFlow.NormalFlow);
    }
}
