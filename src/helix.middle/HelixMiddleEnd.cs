using Helix.Common;
using Helix.Common.Hmm;
using Helix.MiddleEnd.Optimizations;

namespace Helix.MiddleEnd {
    public class HelixMiddleEnd {
        public IReadOnlyList<IHmmSyntax> TypeCheck(IReadOnlyList<IHmmSyntax> lines) {
            var context = new AnalysisContext();

            foreach (var line in lines) {
                line.Accept(context.TypeChecker);
            }

            var writtenLines = context.Writer.AllLines;

            //return writtenLines;

            var deadCodeEliminator = new HmmDeadCodeEliminator();

            return writtenLines.SelectMany(x => x.Accept(deadCodeEliminator)).ToArray();
        }
    }
}
