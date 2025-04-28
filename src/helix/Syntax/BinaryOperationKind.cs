namespace Helix.Syntax;

public enum BinaryOperationKind {
    Add, Subtract, Multiply, Modulo, FloorDivide,
    And, Or, Xor,
    EqualTo, NotEqualTo,
    GreaterThan, LessThan,
    GreaterThanOrEqualTo, LessThanOrEqualTo
}

public static class BinaryOperationExtensions {
    public static string GetSymbol(this BinaryOperationKind kind) {
        return kind switch {
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
    }
}