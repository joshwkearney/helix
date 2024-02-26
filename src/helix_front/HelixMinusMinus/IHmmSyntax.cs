using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    internal interface IHmmSyntax {
        public string VariableName { get; }
    }

    public record HmmWordLiteral : IHmmSyntax {
        public required long Value { get; init; }

        public required string VariableName { get; init; }
    }

    public record HmmBoolLiteral : IHmmSyntax {
        public required string VariableName { get; init; }


    }
}
