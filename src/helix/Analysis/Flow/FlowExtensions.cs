using Helix.Syntax;

namespace Helix.Analysis.Flow {
    public static class FlowExtensions {
        public static bool IsFlowAnalyzed(this ISyntaxTree syntax, FlowFrame flow) {
            return flow.Lifetimes.ContainsKey(syntax);
        }

        public static LifetimeBundle GetLifetimes(this ISyntaxTree syntax, FlowFrame flow) {
            return flow.Lifetimes[syntax];
        }

        public static void SetLifetimes(this ISyntaxTree syntax, LifetimeBundle bundle, FlowFrame flow) {
            flow.Lifetimes[syntax] = bundle;
        }
    }
}