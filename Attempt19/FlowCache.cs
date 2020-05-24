namespace Attempt19 {
    public class FlowCache {
        public ImmutableGraph<IdentifierPath> DependentVariables { get; set; }

        public ImmutableGraph<IdentifierPath> CapturedVariables { get; set; }
    }
}
