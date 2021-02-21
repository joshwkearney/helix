using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public enum BinaryOperation {
        Add, Subtract, Multiply,
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

        public ISyntaxB CheckNames(INameRecorder names) {
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

        private static readonly Dictionary<BinaryOperation, TrophyType> intOperations
            = new Dictionary<BinaryOperation, TrophyType>() {

            { BinaryOperation.Add, TrophyType.Integer },
            { BinaryOperation.Subtract, TrophyType.Integer },
            { BinaryOperation.Multiply, TrophyType.Integer },
            { BinaryOperation.And, TrophyType.Integer },
            { BinaryOperation.Or, TrophyType.Integer },
            { BinaryOperation.Xor, TrophyType.Integer },
            { BinaryOperation.EqualTo, TrophyType.Boolean },
            { BinaryOperation.NotEqualTo, TrophyType.Boolean },
            { BinaryOperation.GreaterThan, TrophyType.Boolean },
            { BinaryOperation.LessThan, TrophyType.Boolean },
            { BinaryOperation.GreaterThanOrEqualTo, TrophyType.Boolean },
            { BinaryOperation.LessThanOrEqualTo, TrophyType.Boolean },
        };

        private static readonly Dictionary<BinaryOperation, TrophyType> boolOperations
            = new Dictionary<BinaryOperation, TrophyType>() {

            { BinaryOperation.And, TrophyType.Boolean },
            { BinaryOperation.Or, TrophyType.Boolean },
            { BinaryOperation.Xor, TrophyType.Boolean },
            { BinaryOperation.EqualTo, TrophyType.Boolean },
            { BinaryOperation.NotEqualTo, TrophyType.Boolean },
        };

        public BinarySyntaxB(TokenLocation loc, ISyntaxB left, ISyntaxB right, BinaryOperation op) {
            this.Location = loc;
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            // Delegate type resolution
            var left = this.left.CheckTypes(types);
            var right = this.right.CheckTypes(types);
            var returnType = TrophyType.Void;

            // Check if left is a valid type
            if (!left.ReturnType.IsIntType && !left.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(this.left.Location, left.ReturnType);
            }

            // Check if right is a valid type
            if (!right.ReturnType.IsIntType && !right.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(this.right.Location, right.ReturnType);
            }

            // Make sure types match
            if (left.ReturnType != right.ReturnType) {
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

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.left.Lifetimes.Union(this.right.Lifetimes);

        public BinarySyntaxC(ISyntaxC left, ISyntaxC right, BinaryOperation op, TrophyType returnType) {
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