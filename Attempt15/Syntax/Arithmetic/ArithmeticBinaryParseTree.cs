using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15.Syntax.Arithmetic {
    public class ArithmeticBinaryParseTree : IParseTree {
        public IParseTree Left { get; }

        public IParseTree Right { get; }

        public ArithmeticBinaryOperationKind Operation { get; }

        public ArithmeticBinaryParseTree(IParseTree left, IParseTree right, ArithmeticBinaryOperationKind kind) {
            this.Left = left;
            this.Right = right;
            this.Operation = kind;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var right = this.Right.Analyze(args);
            var left = this.Left.Analyze(args);

            if (this.Operation == ArithmeticBinaryOperationKind.StrictDivision) {
                return this.AnalyzeStrictDivision(args, right, left);
            }
            else if (this.Operation == ArithmeticBinaryOperationKind.Division) {
                return this.AnalyzeDivision(args, right, left);
            }
            else if (this.Operation == ArithmeticBinaryOperationKind.Spaceship) {
                return this.AnalyzeSpaceship(args, right, left);
            }
            else if (this.Operation == ArithmeticBinaryOperationKind.LessThan
                || this.Operation == ArithmeticBinaryOperationKind.GreaterThan
                || this.Operation == ArithmeticBinaryOperationKind.EqualTo
            ) {
                return this.AnalyzeEquality(args, right, left);
            }
            else {
                return this.AnalyzeOther(args, right, left);
            }
        }

        private ISyntaxTree AnalyzeDivision(AnalyzeEventArgs args, ISyntaxTree right, ISyntaxTree left) {
            if (!args.Unifier.TryUnifySyntax(right, left.ExpressionType, out right)) {
                throw new Exception();
            }

            if (!args.Unifier.TryUnifySyntax(left, right.ExpressionType, out left)) { 
                throw new Exception();
            }

            if (right.ExpressionType.Kind == TrophyTypeKind.Int) {
                if (!args.Unifier.TryUnifySyntax(left, SimpleType.Float, out left)) {
                    throw new Exception();
                }

                if (!args.Unifier.TryUnifySyntax(right, SimpleType.Float, out right)) {
                    throw new Exception();
                }
            }

            return new ArithmeticBinarySyntaxTree(
                left: left,
                right: right,
                operation: ArithmeticBinaryOperationKind.Division,
                returnType: SimpleType.Float
            );
        }

        private ISyntaxTree AnalyzeStrictDivision(AnalyzeEventArgs args, ISyntaxTree right, ISyntaxTree left) {
            if (args.Unifier.TryUnifySyntax(right, SimpleType.Int, out var result)) {
                right = result;
            }
            else {
                throw new Exception();
            }

            if (args.Unifier.TryUnifySyntax(left, SimpleType.Int, out result)) {
                left = result;
            }
            else {
                throw new Exception();
            }

            return new ArithmeticBinarySyntaxTree(
                left:       left,
                right:      right,
                operation:  ArithmeticBinaryOperationKind.StrictDivision,
                returnType: new SimpleType(TrophyTypeKind.Int)
            );
        }

        private ISyntaxTree AnalyzeEquality(AnalyzeEventArgs args, ISyntaxTree right, ISyntaxTree left) {
            if (args.Unifier.TryUnifySyntax(left, right.ExpressionType, out var result)) {
                left = result;
            }
            else if (args.Unifier.TryUnifySyntax(right, left.ExpressionType, out result)) {
                right = result;
            }
            else {
                throw new Exception();
            }

            if (left.ExpressionType.Kind != TrophyTypeKind.Int && left.ExpressionType.Kind != TrophyTypeKind.Float) {
                throw new Exception();
            }

            if (right.ExpressionType.Kind != TrophyTypeKind.Int && right.ExpressionType.Kind != TrophyTypeKind.Float) {
                throw new Exception();
            }

            return new ArithmeticBinarySyntaxTree(
                right:      right,
                left:       left,
                operation:  this.Operation,
                returnType: new SimpleType(TrophyTypeKind.Boolean)
            );
        }

        private ISyntaxTree AnalyzeSpaceship(AnalyzeEventArgs args, ISyntaxTree right, ISyntaxTree left) {
            if (args.Unifier.TryUnifySyntax(left, right.ExpressionType, out var result)) {
                left = result;
            }
            else if (args.Unifier.TryUnifySyntax(right, left.ExpressionType, out result)) {
                right = result;
            }
            else {
                throw new Exception();
            }

            if (left.ExpressionType.Kind != TrophyTypeKind.Int && left.ExpressionType.Kind != TrophyTypeKind.Float) {
                throw new Exception();
            }

            if (right.ExpressionType.Kind != TrophyTypeKind.Int && right.ExpressionType.Kind != TrophyTypeKind.Float) {
                throw new Exception();
            }

            return new ArithmeticBinarySyntaxTree(
                right:      right,
                left:       left,
                operation:  ArithmeticBinaryOperationKind.Spaceship,
                returnType: right.ExpressionType
            );
        }

        private ISyntaxTree AnalyzeOther(AnalyzeEventArgs args, ISyntaxTree right, ISyntaxTree left) {
            if (args.Unifier.TryUnifySyntax(left, right.ExpressionType, out var result)) {
                left = result;
            }
            else if (args.Unifier.TryUnifySyntax(right, left.ExpressionType, out result)) {
                right = result;
            }
            else {
                throw new Exception();
            }

            if (left.ExpressionType.Kind != TrophyTypeKind.Int && left.ExpressionType.Kind != TrophyTypeKind.Float) {
                throw new Exception();
            }

            if (right.ExpressionType.Kind != TrophyTypeKind.Int && right.ExpressionType.Kind != TrophyTypeKind.Float) {
                throw new Exception();
            }

            return new ArithmeticBinarySyntaxTree(
                right:      right,
                left:       left,
                operation:  this.Operation,
                returnType: right.ExpressionType
            );
        }
    }
}