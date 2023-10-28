using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record HmmAssignment : IHmmStatement {
        public HmmVariable Variable { get; init; }

        public HmmValue Value { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {
            return new[] { $"{this.Variable.Name} = {this.Value};" };
        }
    }
}
