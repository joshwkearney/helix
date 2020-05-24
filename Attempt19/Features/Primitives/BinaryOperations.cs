using System.Collections.Immutable;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Primitives;
using System.Collections.Generic;
using Attempt19.TypeChecking;
using System;

namespace Attempt19 {
    public enum BinaryOperation {
        Add, Subtract, Multiply,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public static partial class SyntaxFactory {
        public static Syntax MakeBinaryExpression(Syntax left, Syntax right, BinaryOperation op, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new BinaryExpressionData() { 
                    Left = left,
                    Right = right,
                    Operation = op,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(BinaryExpressionTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Primitives {

    public class BinaryExpressionData : IParsedData, ITypeCheckedData, IFlownData {
        public Syntax Left { get; set; }

        public Syntax Right { get; set; }

        public BinaryOperation Operation { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> EscapingVariables { get; set; }
    }    

    public static class BinaryExpressionTransformations {
        private static readonly Dictionary<BinaryOperation, LanguageType> intOperations
            = new Dictionary<BinaryOperation, LanguageType>() {

            { BinaryOperation.Add, IntType.Instance },
            { BinaryOperation.Subtract, IntType.Instance },
            { BinaryOperation.Multiply, IntType.Instance },
            { BinaryOperation.And, IntType.Instance },
            { BinaryOperation.Or, IntType.Instance },
            { BinaryOperation.Xor, IntType.Instance },
            { BinaryOperation.EqualTo, BoolType.Instance },
            { BinaryOperation.NotEqualTo, BoolType.Instance },
            { BinaryOperation.GreaterThan, BoolType.Instance },
            { BinaryOperation.LessThan, BoolType.Instance },
            { BinaryOperation.GreaterThanOrEqualTo, BoolType.Instance },
            { BinaryOperation.LessThanOrEqualTo, BoolType.Instance },
        };

        private static readonly Dictionary<BinaryOperation, LanguageType> boolOperations
            = new Dictionary<BinaryOperation, LanguageType>() {

            { BinaryOperation.And, BoolType.Instance },
            { BinaryOperation.Or, BoolType.Instance },
            { BinaryOperation.Xor, BoolType.Instance },
            { BinaryOperation.EqualTo, BoolType.Instance },
            { BinaryOperation.NotEqualTo, BoolType.Instance },
        };

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope,
                NameCache names) {

            var bin = (BinaryExpressionData)data;

            // Delegate name declaration
            bin.Left = bin.Left.DeclareNames(scope, names);
            bin.Right = bin.Right.DeclareNames(scope, names);

            return new Syntax() {
                Data = SyntaxData.From(bin),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var bin = (BinaryExpressionData)data;

            // Delegate name resolution
            bin.Left = bin.Left.ResolveNames(names);
            bin.Right = bin.Right.ResolveNames(names);

            return new Syntax() {
                Data = SyntaxData.From(bin),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var bin = (BinaryExpressionData)data;

            // Delegate type declaration
            bin.Left = bin.Left.DeclareTypes(types);
            bin.Right = bin.Right.DeclareTypes(types);

            return new Syntax() {
                Data = SyntaxData.From(bin),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types) {
            var bin = (BinaryExpressionData)data;

            // Delegate type resolution
            bin.Left = bin.Left.ResolveTypes(types);
            bin.Right = bin.Right.ResolveTypes(types);

            var leftType = bin.Left.Data.AsTypeCheckedData().GetValue().ReturnType;
            var rightType = bin.Right.Data.AsTypeCheckedData().GetValue().ReturnType;

            // Check if left is a valid type
            if (leftType != IntType.Instance && leftType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    bin.Left.Data.AsParsedData().Location, 
                    leftType);
            }

            // Check if right is a valid type
            if (rightType != IntType.Instance && rightType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    bin.Right.Data.AsParsedData().Location,
                    rightType);
            }

            // Make sure types match
            if (leftType != rightType) {
                throw TypeCheckingErrors.UnexpectedType(
                    bin.Right.Data.AsParsedData().Location,
                    leftType,
                    rightType);
            }

            // Make sure this is a valid operation
            if (leftType == IntType.Instance) {
                if (!intOperations.TryGetValue(bin.Operation, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(
                        bin.Left.Data.AsParsedData().Location,
                        leftType);
                }

                bin.ReturnType = ret;
            }
            else if (leftType == BoolType.Instance) {
                if (!boolOperations.TryGetValue(bin.Operation, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(
                        bin.Left.Data.AsParsedData().Location,
                        leftType);
                }

                bin.ReturnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            // Set return type
            bin.ReturnType = VoidType.Instance;

            return new Syntax() {
                Data = SyntaxData.From(bin),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, FlowCache flows) {
            var bin = (BinaryExpressionData)data;

            // Delegate flow analysis
            bin.Left = bin.Left.AnalyzeFlow(flows);
            bin.Right = bin.Right.AnalyzeFlow(flows);

            var leftEscape = bin.Left.Data.AsFlownData().GetValue().EscapingVariables;
            var rightEscape = bin.Right.Data.AsFlownData().GetValue().EscapingVariables;

            bin.EscapingVariables = leftEscape.Union(rightEscape);

            return new Syntax() {
                Data = SyntaxData.From(bin),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var bin = (BinaryExpressionData)data;
            var type = bin.Left.Data.AsTypeCheckedData().GetValue().ReturnType;

            string op;

            if (type == IntType.Instance) {
                op = bin.Operation switch {
                    BinaryOperation.Add => " + ",
                    BinaryOperation.Subtract => " - ",
                    BinaryOperation.Multiply => " * ",
                    BinaryOperation.And => " & ",
                    BinaryOperation.Or => " | ",
                    BinaryOperation.Xor => " ^ ",
                    BinaryOperation.GreaterThan => " > ",
                    BinaryOperation.LessThan => " < ",
                    BinaryOperation.GreaterThanOrEqualTo => " >= ",
                    BinaryOperation.LessThanOrEqualTo => " <= ",
                    BinaryOperation.EqualTo => " == ",
                    BinaryOperation.NotEqualTo => " != ",
                    _ => throw new Exception("This should never happen"),
                };
            }
            else if (type == BoolType.Instance) {
                op = bin.Operation switch {
                    BinaryOperation.And => " && ",
                    BinaryOperation.Or => " || ",
                    BinaryOperation.Xor => " != ",
                    BinaryOperation.EqualTo => " == ",
                    BinaryOperation.NotEqualTo => " != ",
                    _ => throw new Exception("This should never happen"),
                };
            }
            else {
                throw new Exception("This should never happen");
            }

            var left = bin.Left.GenerateCode(scope, gen);
            var right = bin.Right.GenerateCode(scope, gen);

            return left.Combine(right, (x, y) => "(" + x + op + y + ")");
        }
    }
}