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

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; set; }
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

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var bin = (BinaryExpressionData)data;

            // Delegate type resolution
            bin.Left = bin.Left.ResolveTypes(types, unifier);
            bin.Right = bin.Right.ResolveTypes(types, unifier);

            var left = bin.Left.Data.AsTypeCheckedData().GetValue();
            var right = bin.Right.Data.AsTypeCheckedData().GetValue();

            // Check if left is a valid type
            if (left.ReturnType != IntType.Instance && left.ReturnType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    bin.Left.Data.AsParsedData().Location, 
                    left.ReturnType);
            }

            // Check if right is a valid type
            if (right.ReturnType != IntType.Instance && right.ReturnType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    bin.Right.Data.AsParsedData().Location,
                    right.ReturnType);
            }

            // Make sure types match
            if (left != right) {
                throw TypeCheckingErrors.UnexpectedType(
                    bin.Right.Data.AsParsedData().Location,
                    left.ReturnType,
                    right.ReturnType);
            }

            // Make sure this is a valid operation
            if (left.ReturnType == IntType.Instance) {
                if (!intOperations.TryGetValue(bin.Operation, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(
                        bin.Left.Data.AsParsedData().Location,
                        left.ReturnType);
                }

                bin.ReturnType = ret;
            }
            else if (left.ReturnType == BoolType.Instance) {
                if (!boolOperations.TryGetValue(bin.Operation, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(
                        bin.Left.Data.AsParsedData().Location,
                        left.ReturnType);
                }

                bin.ReturnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            // Set return type
            bin.ReturnType = VoidType.Instance;

            // Set escaping variables
            bin.EscapingVariables = left.EscapingVariables.Union(right.EscapingVariables);

            return new Syntax() {
                Data = SyntaxData.From(bin),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var bin = (BinaryExpressionData)data;

            // Delegate flow analysis
            bin.Left = bin.Left.AnalyzeFlow(types, flows);
            bin.Right = bin.Right.AnalyzeFlow(types, flows);            

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