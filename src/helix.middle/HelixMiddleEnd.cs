using Helix.Common;
using Helix.Common.Hmm;
using Helix.MiddleEnd.Optimizations;

namespace Helix.MiddleEnd {
    public class HelixMiddleEnd {
        public IReadOnlyList<IHirSyntax> TypeCheck(IReadOnlyList<IHmmSyntax> lines) {
            var context = new AnalysisContext();
            foreach (var line in lines) {
                line.Accept(context.TypeChecker);
            }

            var deadCodeEliminator = new HirDeadCodeEliminator();
            foreach (var line in context.Writer.AllLines) {
                line.Accept(deadCodeEliminator);
            }

            return context.Writer.AllLines;
        }
    }
}
