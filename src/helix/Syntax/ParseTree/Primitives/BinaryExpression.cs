using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.TypeChecking.Predicates;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Primitives;

public record BinaryExpression : IParseExpression {
    private static readonly Dictionary<BinaryOperationKind, HelixType> intOperations = new() {
        { BinaryOperationKind.Add,                  PrimitiveType.Word },
        { BinaryOperationKind.Subtract,             PrimitiveType.Word },
        { BinaryOperationKind.Multiply,             PrimitiveType.Word },
        { BinaryOperationKind.Modulo,               PrimitiveType.Word },
        { BinaryOperationKind.FloorDivide,          PrimitiveType.Word },
        { BinaryOperationKind.And,                  PrimitiveType.Word },
        { BinaryOperationKind.Or,                   PrimitiveType.Word },
        { BinaryOperationKind.Xor,                  PrimitiveType.Word },
        { BinaryOperationKind.EqualTo,              PrimitiveType.Bool },
        { BinaryOperationKind.NotEqualTo,           PrimitiveType.Bool },
        { BinaryOperationKind.GreaterThan,          PrimitiveType.Bool },
        { BinaryOperationKind.LessThan,             PrimitiveType.Bool },
        { BinaryOperationKind.GreaterThanOrEqualTo, PrimitiveType.Bool },
        { BinaryOperationKind.LessThanOrEqualTo,    PrimitiveType.Bool },
    };

    private static readonly Dictionary<BinaryOperationKind, HelixType> boolOperations = new() {
        { BinaryOperationKind.And,                  PrimitiveType.Bool },
        { BinaryOperationKind.Or,                   PrimitiveType.Bool },
        { BinaryOperationKind.Xor,                  PrimitiveType.Bool },
        { BinaryOperationKind.EqualTo,              PrimitiveType.Bool },
        { BinaryOperationKind.NotEqualTo,           PrimitiveType.Bool },
    };

    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Left { get; init; }
        
    public required IParseExpression Right { get; init; }
        
    public required BinaryOperationKind Operator { get; init; }
        
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        (var left, types) = this.Left.CheckTypes(types);
        (var right, types) = this.Right.CheckTypes(types);

        if (left.ReturnType.IsBool(types) && right.ReturnType.IsBool(types)) {
            return this.CheckBoolExpresion(left, right, types);
        }
        else if (left.ReturnType.IsWord(types) || right.ReturnType.IsWord(types)) {
            return this.CheckIntExpresion(left, right, types);
        }
        else {
            throw TypeException.UnexpectedType(this.Right.Location, left.ReturnType);
        }
    }

    private TypeCheckResult<ITypedExpression> CheckIntExpresion(ITypedExpression left, ITypedExpression right, TypeFrame types) {
        if (!intOperations.TryGetValue(this.Operator, out var returnType)) {
            throw TypeException.UnexpectedType(this.Left.Location, left.ReturnType);
        }

        var leftType = left.ReturnType;
        var rightType = right.ReturnType;

        if (leftType is SingularWordType singLeft && rightType is SingularWordType singRight) {
            return this.EvaluateIntExpression(singLeft.Value, singRight.Value, types);
        }

        left = left.UnifyTo(PrimitiveType.Word, types);
        right = right.UnifyTo(PrimitiveType.Word, types);

        var result = new TypedBinaryExpression {
            Location = this.Location,
            Left = left,
            Right = right,
            Operator = this.Operator,
            ReturnType = returnType,
        };
            
        return new TypeCheckResult<ITypedExpression>(result, types);
    }

    private TypeCheckResult<ITypedExpression> EvaluateIntExpression(long int1, long int2, TypeFrame types) {
        HelixType returnType;

        switch (this.Operator) {
            case BinaryOperationKind.Add:
                returnType = new SingularWordType(int1 + int2);
                break;
            case BinaryOperationKind.Subtract:
                returnType = new SingularWordType(int1 - int2);
                break;
            case BinaryOperationKind.Modulo:
                returnType = new SingularWordType(int1 % int2);
                break;
            case BinaryOperationKind.FloorDivide:
                returnType = new SingularWordType(int1 / int2);
                break;
            case BinaryOperationKind.And:
                returnType = new SingularWordType(int1 & int2);
                break;
            case BinaryOperationKind.Or:
                returnType = new SingularWordType(int1 | int2);
                break;
            case BinaryOperationKind.Xor:
                returnType = new SingularWordType(int1 ^ int2);
                break;
            case BinaryOperationKind.EqualTo:
                returnType = new SingularBoolType(int1 == int2);
                break;
            case BinaryOperationKind.NotEqualTo:
                returnType = new SingularBoolType(int1 != int2);
                break;
            case BinaryOperationKind.GreaterThan:
                returnType = new SingularBoolType(int1 > int2);
                break;
            case BinaryOperationKind.LessThan:
                returnType = new SingularBoolType(int1 < int2);
                break;
            case BinaryOperationKind.GreaterThanOrEqualTo:
                returnType = new SingularBoolType(int1 >= int2);
                break;
            case BinaryOperationKind.LessThanOrEqualTo:
                returnType = new SingularBoolType(int1 <= int2);
                break;
            default:
                throw new Exception();
        }

        var result = returnType.ToSyntax(this.Location, types).GetValue();
            
        return new TypeCheckResult<ITypedExpression>(result, types);
    }

    private TypeCheckResult<ITypedExpression> CheckBoolExpresion(ITypedExpression left, ITypedExpression right, TypeFrame types) {
        var leftType = left.ReturnType;
        var rightType = right.ReturnType;

        if (!boolOperations.TryGetValue(this.Operator, out var ret)) {
            throw TypeException.UnexpectedType(this.Left.Location, leftType);
        }

        var predicate = ISyntaxPredicate.Empty;
        var returnType = PrimitiveType.Bool as HelixType;

        if (leftType is PredicateBool leftPred && rightType is PredicateBool rightPred) {
            switch (this.Operator) {
                case BinaryOperationKind.And:
                    predicate = leftPred.Predicate.And(rightPred.Predicate);
                    break;
                case BinaryOperationKind.Or:
                    predicate = leftPred.Predicate.Or(rightPred.Predicate);
                    break;
                case BinaryOperationKind.NotEqualTo:
                case BinaryOperationKind.Xor:
                    predicate = leftPred.Predicate.Xor(rightPred.Predicate);
                    break;
                case BinaryOperationKind.EqualTo:
                    predicate = leftPred.Predicate.Xor(rightPred.Predicate).Negate();
                    break;
            }

            returnType = new PredicateBool(predicate);
        }

        if (leftType is SingularBoolType singLeft && rightType is SingularBoolType singRight) {
            return this.EvaluateBoolExpression(singLeft.Value, singRight.Value, predicate, types);
        }

        var result = new TypedBinaryExpression {
            Location = this.Location,
            Left = left,
            Right = right,
            Operator = this.Operator,
            ReturnType = returnType
        };
            
        return new TypeCheckResult<ITypedExpression>(result, types);
    }

    private TypeCheckResult<ITypedExpression> EvaluateBoolExpression(bool b1, bool b2, ISyntaxPredicate pred, TypeFrame types) {
        bool value;

        switch (this.Operator) {
            case BinaryOperationKind.And:
                value = b1 & b2;
                break;
            case BinaryOperationKind.Or:
                value = b1 | b2;
                break;
            case BinaryOperationKind.NotEqualTo:
            case BinaryOperationKind.Xor:
                value = b1 ^ b2;
                break;
            case BinaryOperationKind.EqualTo:
                value = b1 == b2;
                break;
            default:
                throw new Exception();
        }

        var returnType = new SingularBoolType(value, pred);
        var result = returnType.ToSyntax(this.Location, types).GetValue();
            
        return new TypeCheckResult<ITypedExpression>(result, types);
    }
}