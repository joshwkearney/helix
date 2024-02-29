using helix.common.Hmm;
using Helix.HelixMinusMinus;
using Helix.MiddleEnd.TypeChecking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
