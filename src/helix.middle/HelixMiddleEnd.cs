using Helix.Common.Hmm;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd {
    public class HelixMiddleEnd {
        public IReadOnlyList<IHmmSyntax> TypeCheck(IReadOnlyList<IHmmSyntax> lines) {
            var context = new TypeCheckingContext();

            foreach (var line in lines) {
                line.Accept(context.TypeChecker);
            }

            return context.Writer.AllLines;
        }
    }
}
