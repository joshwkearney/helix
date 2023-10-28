using Helix.Features.Primitives;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record UnaryStatement : IHmmStatement {
        public HmmVariable ResultVariable { get; init; }

        public HmmValue Operand { get; init; }

        public UnaryOperatorKind Operation { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {
            var op = this.Operation switch {
                UnaryOperatorKind.Minus => "-",
                UnaryOperatorKind.Not => "!",
                UnaryOperatorKind.Plus => "+",
                _ => throw new NotImplementedException()
            };

            return new[] { $"let {this.ResultVariable.Name} as {this.ResultVariable.Type} = {this.Operand};" };
        }
    }
}
