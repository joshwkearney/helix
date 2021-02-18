using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

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

        private static readonly Dictionary<BinaryOperation, LanguageType> intOperations
            = new Dictionary<BinaryOperation, LanguageType>() {

            { BinaryOperation.Add, LanguageType.Integer },
            { BinaryOperation.Subtract, LanguageType.Integer },
            { BinaryOperation.Multiply, LanguageType.Integer },
            { BinaryOperation.And, LanguageType.Integer },
            { BinaryOperation.Or, LanguageType.Integer },
            { BinaryOperation.Xor, LanguageType.Integer },
            { BinaryOperation.EqualTo, LanguageType.Boolean },
            { BinaryOperation.NotEqualTo, LanguageType.Boolean },
            { BinaryOperation.GreaterThan, LanguageType.Boolean },
            { BinaryOperation.LessThan, LanguageType.Boolean },
            { BinaryOperation.GreaterThanOrEqualTo, LanguageType.Boolean },
            { BinaryOperation.LessThanOrEqualTo, LanguageType.Boolean },
        };

        private static readonly Dictionary<BinaryOperation, LanguageType> boolOperations
            = new Dictionary<BinaryOperation, LanguageType>() {

            { BinaryOperation.And, LanguageType.Boolean },
            { BinaryOperation.Or, LanguageType.Boolean },
            { BinaryOperation.Xor, LanguageType.Boolean },
            { BinaryOperation.EqualTo, LanguageType.Boolean },
            { BinaryOperation.NotEqualTo, LanguageType.Boolean },
        };

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.LeftArgument = this.LeftArgument.CheckNames(names);
            this.RightArgument = this.RightArgument.CheckNames(names);

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            // Delegate type resolution
            var left = this.LeftArgument.CheckTypes(names, types);
            var right = this.RightArgument.CheckTypes(names, types);
            var returnType = LanguageType.Void;

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

    public class BinaryTypeCheckedSyntax : ITypeCheckedSyntax {
        public ITypeCheckedSyntax LeftArgument { get; set; }

        public ITypeCheckedSyntax RightArgument { get; set; }

        public BinaryOperation Operation { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var left = this.LeftArgument.GenerateCode(declWriter, statWriter);
            var right = this.RightArgument.GenerateCode(declWriter, statWriter);

            return CExpression.BinaryExpression(left, right, this.Operation);
        }
    }
}