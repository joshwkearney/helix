using Helix.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record HmmIfStatement : IHmmStatement {
        public HmmValue Condition { get; init; }

        public IReadOnlyList<IHmmStatement> TrueStatements { get; init; }

        public IReadOnlyList<IHmmStatement> FalseStatements { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {
            if (!this.TrueStatements.Any() && !this.FalseStatements.Any()) {
                return new[] { "void;" };
            }

            if (!this.FalseStatements.Any()) {
                var stats = this.TrueStatements
                    .SelectMany(x => x.Write())
                    .Select(x => "    " + x)
                    .ToArray();

                return 
                    new[] { $"if {this.Condition} then {{" }
                    .Concat(stats)
                    .Append("};")
                    .ToArray();
            }
            else if (!this.TrueStatements.Any()) {
                var stats = this.FalseStatements
                    .SelectMany(x => x.Write())
                    .Select(x => "    " + x)
                    .ToArray();

                return
                    new[] { $"if !{this.Condition} then {{" }
                    .Concat(stats)
                    .Append("};")
                    .ToArray();
            }
            else {
                var trueStats = this.TrueStatements
                   .SelectMany(x => x.Write())
                   .Select(x => "    " + x)
                   .ToArray();

                var falseStats = this.FalseStatements
                   .SelectMany(x => x.Write())
                   .Select(x => "    " + x)
                   .ToArray();

                return
                    new[] { $"if {this.Condition} then {{" }
                    .Concat(trueStats)
                    .Append("}")
                    .Append("else {")
                    .Concat(falseStats)
                    .Append("};")
                    .ToArray();
            }
        }
    }
}
