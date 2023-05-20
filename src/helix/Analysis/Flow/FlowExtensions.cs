using Helix.Syntax;

namespace Helix.Analysis.Flow {
    public static class FlowExtensions {
        public static bool IsFlowAnalyzed(this ISyntaxTree syntax, FlowFrame flow) {
            return flow.SyntaxLifetimes.ContainsKey(syntax);
        }

        public static LifetimeBundle GetLifetimes(this ISyntaxTree syntax, FlowFrame flow) {
            return flow.SyntaxLifetimes[syntax];
        }

        public static void SetLifetimes(this ISyntaxTree syntax, LifetimeBundle bundle, FlowFrame flow) {
            flow.SyntaxLifetimes[syntax] = bundle;
        }
    }
}