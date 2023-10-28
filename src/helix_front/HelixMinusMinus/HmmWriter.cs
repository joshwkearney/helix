using Helix.Analysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public class HmmWriter {
        private readonly HmmWriter previous = null;
        private readonly List<IHmmStatement> stats = new List<IHmmStatement>();
        private int tempCounter = 0;

        public IReadOnlyList<IHmmStatement> Statements => this.stats;

        public HmmWriter() { }

        public HmmWriter(HmmWriter prev) {
            this.previous = prev;
        }

        public HmmWriter AddStatement(IHmmStatement stat) {
            this.stats.Add(stat);

            return this;
        }

        public string GetTempVariable() {
            if (this.previous != null) {
                return this.previous.GetTempVariable();
            }

            return "%" + this.tempCounter++;
        }

        public HmmVariable GetTempVariable(HelixType type) {
            return new HmmVariable() {
                Name = this.GetTempVariable(),
                Type = type
            };
        }

        public override string ToString() {
            return string.Join("\n", this.Statements.SelectMany(x => x.Write()));
        }
    }
}
