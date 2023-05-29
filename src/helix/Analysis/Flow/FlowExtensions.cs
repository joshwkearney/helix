using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Flow {
    public static class FlowExtensions {
        public static LifetimeBounds GetLifetimes(this ISyntaxTree syntax, TypeFrame flow) {
            return flow.SyntaxLifetimes[syntax];
        }

        public static void SetLifetimes(this ISyntaxTree syntax, LifetimeBounds bounds, TypeFrame flow) {
            flow.SyntaxLifetimes[syntax] = bounds;
        }
    }
}