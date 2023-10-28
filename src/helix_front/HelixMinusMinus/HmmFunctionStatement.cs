using Helix.Features.Types;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record HmmFunctionStatement : IHmmStatement {
        public FunctionType Signature { get; init; }

        public string Name { get; init; }

        public IReadOnlyList<IHmmStatement> Body { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {
            var stats = this.Body
                .SelectMany(x => x.Write())
                .Select(x => "    " + x)
                .ToArray();

            var pars = this.Signature.Parameters
                .Select(x => x.Name + " as " + x.Type)
                .ToArray();

            return new[] { $"func {this.Name}({string.Join(", ", pars)}) as {this.Signature.ReturnType} {{" }
                .Concat(stats)
                .Append("};")
                .ToArray();
        }
    }
}
