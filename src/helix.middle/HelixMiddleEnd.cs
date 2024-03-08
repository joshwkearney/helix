using Helix.Common.Hmm;

namespace Helix.MiddleEnd {
    public class HelixMiddleEnd {
        public IReadOnlyList<IHmmSyntax> TypeCheck(IReadOnlyList<IHmmSyntax> lines) {
            var context = new AnalysisContext();

            foreach (var line in lines) {
                line.Accept(context.TypeChecker);
            }

            return context.Writer.AllLines;
        }
    }
}
