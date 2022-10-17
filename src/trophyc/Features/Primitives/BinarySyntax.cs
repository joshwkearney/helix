using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public enum BinaryOperation {
        Add, Subtract, Multiply, Modulo, FloorDivide,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public class BinarySyntaxA : ISyntaxA {
        private readonly ISyntaxA left, right;
        private readonly BinaryOperation op;

        public TokenLocation Location { get; }

        public BinarySyntaxA(TokenLocation loc, ISyntaxA left, ISyntaxA right, BinaryOperation op) {
            this.Location = loc;
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            return new BinarySyntaxB(
                loc: this.Location,
                left: this.left.CheckNames(names),
                right: this.right.CheckNames(names),
                op: this.op);
        }
    }

    public class BinarySyntaxB : ISyntaxB {
        private readonly ISyntaxB left, right;
        private readonly BinaryOperation op;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.left.VariableUsage.Union(this.right.VariableUsage);
        }

        private static readonly Dictionary<BinaryOperation, ITrophyType> intOperations
            = new Dictionary<BinaryOperation, ITrophyType>() {

            { BinaryOperation.Add, ITrophyType.Integer },
            { BinaryOperation.Subtract, ITrophyType.Integer },
            { BinaryOperation.Multiply, ITrophyType.Integer },
            { BinaryOperation.Modulo, ITrophyType.Integer },
            { BinaryOperation.FloorDivide, ITrophyType.Integer },
            { BinaryOperation.And, ITrophyType.Integer },
            { BinaryOperation.Or, ITrophyType.Integer },
            { BinaryOperation.Xor, ITrophyType.Integer },
            { BinaryOperation.EqualTo, ITrophyType.Boolean },
            { BinaryOperation.NotEqualTo, ITrophyType.Boolean },
            { BinaryOperation.GreaterThan, ITrophyType.Boolean },
            { BinaryOperation.LessThan, ITrophyType.Boolean },
            { BinaryOperation.GreaterThanOrEqualTo, ITrophyType.Boolean },
            { BinaryOperation.LessThanOrEqualTo, ITrophyType.Boolean },
        };

        private static readonly Dictionary<BinaryOperation, ITrophyType> boolOperations
            = new Dictionary<BinaryOperation, ITrophyType>() {

            { BinaryOperation.And, ITrophyType.Boolean },
            { BinaryOperation.Or, ITrophyType.Boolean },
            { BinaryOperation.Xor, ITrophyType.Boolean },
            { BinaryOperation.EqualTo, ITrophyType.Boolean },
            { BinaryOperation.NotEqualTo, ITrophyType.Boolean },
        };

        public BinarySyntaxB(TokenLocation loc, ISyntaxB left, ISyntaxB right, BinaryOperation op) {
            this.Location = loc;
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            // Delegate type resolution
            var left = this.left.CheckTypes(types);
            var right = this.right.CheckTypes(types);
            var returnType = ITrophyType.Void;

            // Check if left is a valid type
            if (!left.ReturnType.IsIntType && !left.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
            }

            // Check if right is a valid type
            if (!right.ReturnType.IsIntType && !right.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, right.ReturnType);
            }

            // Make sure types match
            if (!left.ReturnType.Equals(right.ReturnType)) {
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, left.ReturnType, right.ReturnType);
            }

            // Make sure this is a valid operation
            if (left.ReturnType.IsIntType) {
                if (!intOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
                }

                returnType = ret;
            }
            else if (left.ReturnType.IsBoolType) {
                if (!boolOperations.TryGetValue(this.op, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
                }

                returnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            return new BinarySyntaxC(left, right, this.op, returnType);
        }
    }

    public class BinarySyntaxC : ISyntaxC {
        private readonly ISyntaxC left, right;
        private readonly BinaryOperation op;

        public ITrophyType ReturnType { get; }

        public BinarySyntaxC(ISyntaxC left, ISyntaxC right, BinaryOperation op, ITrophyType returnType) {
            this.left = left;
            this.right = right;
            this.op = op;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var left = this.left.GenerateCode(declWriter, statWriter);
            var right = this.right.GenerateCode(declWriter, statWriter);

            return CExpression.BinaryExpression(left, right, this.op);
        }
    }
}