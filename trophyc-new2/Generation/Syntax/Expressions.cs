using Trophy.Features.Primitives;

namespace Trophy.Generation.Syntax {
    public interface ICSyntax {
        public string WriteToC();
    }

    public record CIntLiteral(int Value) : ICSyntax {
        public string WriteToC() => this.Value + "U";
    }

    public record CCompoundExpression : ICSyntax {
        public IEnumerable<ICSyntax>? Arguments { get; init; } = null;

        public string WriteToC() {
            var args = string.Join(", ", this.Arguments!.Select(x => x.WriteToC()));

            return "{ " + args + " }";
        }
    }

    public record CBinaryExpression() : ICSyntax {
        public ICSyntax? Left { get; init; } = null;

        public ICSyntax? Right { get; init; } = null;

        public BinaryOperationKind? Operation { get; init; } = null;

        public string WriteToC() {
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

            return "(" + this.Left!.WriteToC() + " " + op + " " + this.Right!.WriteToC() + ")";
        }
    }

    public record CVariableLiteral(string Name) : ICSyntax {
        public string WriteToC() => this.Name;
    }

    public record CPointerDereference() : ICSyntax {
        public ICSyntax? Target { get; init; } = null;

        public string WriteToC() {
            var target = this.Target!.WriteToC();

            if (target.StartsWith("&")) {
                return target.Substring(1);
            }

            return "*" + target;
        }
    }

    public record CNot() : ICSyntax {
        public ICSyntax? Target { get; init; } = null;

        public string WriteToC() => "!" + this.Target!.WriteToC();
    }

    public record CAddressOf() : ICSyntax {
        public ICSyntax? Target { get; init; } = null;

        public string WriteToC() {
            var target = this.Target!.WriteToC();

            if (target.StartsWith("*")) {
                return target.Substring(1);
            }

            return "&" + target;
        }
    }

    public record CInvoke() : ICSyntax {
        public ICSyntax? Target { get; init; } = null;

        public IEnumerable<ICSyntax> Arguments { get; init; } = Array.Empty<ICSyntax>();

        public string WriteToC() {
            var args = string.Join(", ", this.Arguments.Select(x => x.WriteToC()));

            return this.Target!.WriteToC() + "(" + args + ")";
        }
    }

    public record CSizeof() : ICSyntax {
        public ICSyntax? Target { get; init; } = null;

        public string WriteToC() => "sizeof(" + this.Target!.WriteToC() + ")";
    }

    public record CStringLiteral(string Value) : ICSyntax {
        public string WriteToC() => "\"" + this.Value + "\"";
    }

    public record CMemberAccess() : ICSyntax {
        public ICSyntax? Target { get; init; } = null;

        public string? MemberName { get; init; } = null;

        public string WriteToC() => this.Target!.WriteToC() + "." + this.MemberName!;
    }

    public record CCast() : ICSyntax {
        public ICSyntax? Type { get; init; } = null;
        public ICSyntax? Target { get; init; } = null;

        public string WriteToC() {
            return "(" + this.Type!.WriteToC() + ")" + this.Target!.WriteToC();
        }
    }
}
