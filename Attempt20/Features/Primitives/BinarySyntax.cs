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

    public class BinaryParseSyntax : IParsedSyntax {
        public IParsedSyntax LeftArgument { get; set; }

        public IParsedSyntax RightArgument { get; set; }

        public BinaryOperation Operation { get; set; }

        public TokenLocation Location { get; set; }

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

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.LeftArgument = this.LeftArgument.CheckNames(names);
            this.RightArgument = this.RightArgument.CheckNames(names);

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            // Delegate type resolution
            var left = this.LeftArgument.CheckTypes(names, types);
            var right = this.RightArgument.CheckTypes(names, types);
            var returnType = TrophyType.Void;

            // Check if left is a valid type
            if (!left.ReturnType.IsIntType && !left.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(left.Location, left.ReturnType);
            }

            // Check if right is a valid type
            if (!right.ReturnType.IsIntType && !right.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(right.Location, right.ReturnType);
            }

            // Make sure types match
            if (left.ReturnType != right.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(right.Location, left.ReturnType, right.ReturnType);
            }

            // Make sure this is a valid operation
            if (left.ReturnType.IsIntType) {
                if (!intOperations.TryGetValue(this.Operation, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(left.Location, left.ReturnType);
                }

                returnType = ret;
            }
            else if (left.ReturnType.IsBoolType) {
                if (!boolOperations.TryGetValue(this.Operation, out var ret)) {
                    throw TypeCheckingErrors.UnexpectedType(left.Location, left.ReturnType);
                }

                returnType = ret;
            }
            else {
                throw new Exception("This should never happen");
            }

            return new BinaryTypeCheckedSyntax() {
                Operation = this.Operation,
                LeftArgument = left,
                RightArgument = right,
                Location = this.Location,
                ReturnType = returnType,
                Lifetimes = left.Lifetimes.Union(right.Lifetimes)
            };
        }
    }

    public class BinaryTypeCheckedSyntax : ISyntax {
        public ISyntax LeftArgument { get; set; }

        public ISyntax RightArgument { get; set; }

        public BinaryOperation Operation { get; set; }

        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var left = this.LeftArgument.GenerateCode(declWriter, statWriter);
            var right = this.RightArgument.GenerateCode(declWriter, statWriter);

            return CExpression.BinaryExpression(left, right, this.Operation);
        }
    }
}