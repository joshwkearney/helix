using Helix.Analysis.Types;
using Helix.HelixMinusMinus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Syntax {
    public class ImperativeSyntaxWriter {
        private readonly ImperativeSyntaxWriter previous = null;
        private readonly List<IImperativeStatement> stats = new List<IImperativeStatement>();
        private int tempCounter = 0;

        public IReadOnlyList<IImperativeStatement> Statements => stats;

        public ImperativeSyntaxWriter() { }

        public ImperativeSyntaxWriter(ImperativeSyntaxWriter prev) {
            previous = prev;
        }

        public ImperativeSyntaxWriter AddStatement(IImperativeStatement stat) {
            stats.Add(stat);

            return this;
        }

        public HmmVariable GetTempVariable() {
            if (previous != null) {
                return previous.GetTempVariable();
            }

            return new HmmVariable() {
                Name = "%" + tempCounter++
            };
        }

        public override string ToString() {
            return string.Join("\n", Statements.SelectMany(x => x.Write()));
        }
    }
}
