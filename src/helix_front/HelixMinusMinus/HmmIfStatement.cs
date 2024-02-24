using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record HmmIfStatement : IHmmStatement {
        public HmmVariable ResultVariable { get; init; }

        public ImperativeExpression TrueValue { get; init; }

        public ImperativeExpression FalseValue { get; init; }

        public ImperativeExpression Condition { get; init; }

        public IReadOnlyList<IHmmStatement> TrueStatements { get; init; }

        public IReadOnlyList<IHmmStatement> FalseStatements { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {            
            var trueStats = this.TrueStatements
                .SelectMany(x => x.Write())
                .Append($"{this.ResultVariable.Name} = {this.TrueValue};")
                .Select(x => "    " + x)
                .ToArray();

            var falseStats = this.FalseStatements
                .SelectMany(x => x.Write())
                .Append($"{this.ResultVariable.Name} = {this.FalseValue};")
                .Select(x => "    " + x)
                .ToArray();

            return
                    new[] {
                    $"var {this.ResultVariable.Name};",
                    $"if {this.Condition} then {{"
                }
                .Concat(trueStats)
                .Append("}")
                .Append("else {")
                .Concat(falseStats)
                .Append("};")
                .ToArray();
        }
    }
}
