using Helix.Common.Hmm;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd {
    public class HelixMiddleEnd {
        public IReadOnlyList<IHmmSyntax> TypeCheck(IReadOnlyList<IHmmSyntax> lines) {
            var writer = new HmmWriter();
            var types = new TypeStore();
            var names = new TypeCheckingNamesStore();

            var context = new TypeCheckingContext() {
                Types = types,
                Writer = writer,
                Names = names
            };

            foreach (var line in lines) {
                line.Accept(context.TypeChecker);
            }

            return writer.Lines;
        }
    }
}
