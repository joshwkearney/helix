using Helix.Features.Primitives;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record BinaryStatement : IHmmStatement {
        public HmmVariable ResultVariable { get; init; }

        public HmmValue Left { get; init; }

        public HmmValue Right { get; init; }

        public BinaryOperationKind Operation { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {
            var op = this.Operation switch {
                BinaryOperationKind.Add => "+",
                BinaryOperationKind.And => "&",
                BinaryOperationKind.EqualTo => "==",
                BinaryOperationKind.GreaterThan => ">",
                BinaryOperationKind.GreaterThanOrEqualTo => ">=",
                BinaryOperationKind.LessThan => "<",
                BinaryOperationKind.LessThanOrEqualTo => "<=",
                BinaryOperationKind.Multiply => "*",
                BinaryOperationKind.NotEqualTo => "!=",
                BinaryOperationKind.Or => "|",
                BinaryOperationKind.Subtract => "-",
                BinaryOperationKind.Xor => "^",
                BinaryOperationKind.Modulo => "%",
                BinaryOperationKind.FloorDivide => "/",
                _ => throw new Exception()
            };

            return new[] { $"let {this.ResultVariable.Name} as {this.ResultVariable.Type} = {this.Left} {op} {this.Right};" };
        }
    }
}
