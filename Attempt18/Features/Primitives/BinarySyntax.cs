using System;
using System.Collections.Generic;
using System.Linq;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.Primitives {
    public enum BinarySyntaxOperation {
        Add, Subtract, Multiply,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public class BinarySyntax : ISyntax {
        private static readonly Dictionary<BinarySyntaxOperation, LanguageType> intOperations
            = new Dictionary<BinarySyntaxOperation, LanguageType>() {

            { BinarySyntaxOperation.Add, IntType.Instance },
            { BinarySyntaxOperation.Subtract, IntType.Instance },
            { BinarySyntaxOperation.Multiply, IntType.Instance },
            { BinarySyntaxOperation.And, IntType.Instance },
            { BinarySyntaxOperation.Or, IntType.Instance },
            { BinarySyntaxOperation.Xor, IntType.Instance },
            { BinarySyntaxOperation.EqualTo, BoolType.Instance },
            { BinarySyntaxOperation.NotEqualTo, BoolType.Instance },
            { BinarySyntaxOperation.GreaterThan, BoolType.Instance },
            { BinarySyntaxOperation.LessThan, BoolType.Instance },
            { BinarySyntaxOperation.GreaterThanOrEqualTo, BoolType.Instance },
            { BinarySyntaxOperation.LessThanOrEqualTo, BoolType.Instance },
        };

        private static readonly Dictionary<BinarySyntaxOperation, LanguageType> boolOperations
            = new Dictionary<BinarySyntaxOperation, LanguageType>() {

            { BinarySyntaxOperation.And, BoolType.Instance },
            { BinarySyntaxOperation.Or, BoolType.Instance },
            { BinarySyntaxOperation.Xor, BoolType.Instance },
            { BinarySyntaxOperation.EqualTo, BoolType.Instance },
            { BinarySyntaxOperation.NotEqualTo, BoolType.Instance },
        };

        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Left { get; set; }

        public ISyntax Right { get; set; }

        public BinarySyntaxOperation Operation { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Left.AnalyzeFlow(types, flow);
            this.Right.AnalyzeFlow(types, flow);

            this.CapturedVariables = this.Left.CapturedVariables
                .Concat(this.Right.CapturedVariables)
                .Distinct()
                .ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Left.DeclareNames(names);
            this.Right.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache cache) {
            this.Left.DeclareTypes(cache);
            this.Right.DeclareTypes(cache);
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var left = this.Left.Evaluate(memory).Value;
            var right = this.Right.Evaluate(memory).Value;

            if (left is long leftInt && right is long rightInt) {
                object result;

                switch (this.Operation) {
                    case BinarySyntaxOperation.Add:
                        result = leftInt + rightInt;
                        break;
                    case BinarySyntaxOperation.Subtract:
                        result = leftInt - rightInt;
                        break;
                    case BinarySyntaxOperation.Multiply:
                        result = leftInt * rightInt;
                        break;
                    case BinarySyntaxOperation.And:
                        result = leftInt & rightInt;
                        break;
                    case BinarySyntaxOperation.Or:
                        result = leftInt | rightInt;
                        break;
                    case BinarySyntaxOperation.Xor:
                        result = leftInt ^ rightInt;
                        break;
                    case BinarySyntaxOperation.EqualTo:
                        result = leftInt == rightInt;
                        break;
                    case BinarySyntaxOperation.NotEqualTo:
                        result = leftInt != rightInt;
                        break;
                    case BinarySyntaxOperation.GreaterThan:
                        result = leftInt > rightInt;
                        break;
                    case BinarySyntaxOperation.LessThan:
                        result = leftInt < rightInt;
                        break;
                    case BinarySyntaxOperation.GreaterThanOrEqualTo:
                        result = leftInt >= rightInt;
                        break;
                    case BinarySyntaxOperation.LessThanOrEqualTo:
                        result = leftInt <= rightInt;
                        break;
                    default:
                        throw new Exception();
                }

                return new AtomicEvaluateResult(result);
            }
            else if (left is bool leftBool && right is bool rightBool) {
                object result;

                switch (this.Operation) {
                    case BinarySyntaxOperation.And:
                        result = leftBool && rightBool;
                        break;
                    case BinarySyntaxOperation.Or:
                        result = leftBool || rightBool;
                        break;
                    case BinarySyntaxOperation.Xor:
                        result = leftBool ^ rightBool;
                        break;
                    case BinarySyntaxOperation.EqualTo:
                        result = leftBool == rightBool;
                        break;
                    case BinarySyntaxOperation.NotEqualTo:
                        result = leftBool != rightBool;
                        break;
                    default:
                        throw new Exception();
                }

                return new AtomicEvaluateResult(result);
            }

            throw new Exception();
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            this.Left.PreEvaluate(memory);
            this.Right.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Left.ResolveNames(names);
            this.Right.ResolveNames(names);
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            this.Left.ResolveScope(containingScope);
            this.Right.ResolveScope(containingScope);
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.Left = this.Left.ResolveTypes(types);
            this.Right = this.Right.ResolveTypes(types);

            // Make sure types match
            if (this.Left.ReturnType != this.Right.ReturnType) {
                throw new Exception();
            }

            // Make sure this is a valid operation
            if (this.Left.ReturnType == IntType.Instance) {
                if (!intOperations.TryGetValue(this.Operation, out var ret)) {
                    throw new Exception();
                }

                this.ReturnType = ret;
            }
            else if (this.Left.ReturnType == BoolType.Instance) {
                if (!boolOperations.TryGetValue(this.Operation, out var ret)) {
                    throw new Exception();
                }

                this.ReturnType = ret;
            }
            else {
                throw new Exception();
            }

            return this;
        }
    }
}
