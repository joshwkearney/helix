using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15.Syntax.Logic {
    public class BooleanBinaryParseTree : IParseTree {
        public IParseTree Right { get; }

        public IParseTree Left { get; }

        public BooleanBinaryOperationKind Operation { get; }

        public BooleanBinaryParseTree(IParseTree left, IParseTree right, BooleanBinaryOperationKind op) {
            this.Right = right;
            this.Left = left;
            this.Operation = op;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var right = this.Right.Analyze(args);
            var left = this.Left.Analyze(args);

            if (!args.Unifier.TryUnifySyntax(right, SimpleType.Boolean, out right)) {
                throw new Exception();
            }

            if (!args.Unifier.TryUnifySyntax(left, SimpleType.Boolean, out left)) {
                throw new Exception();
            }

            return new BooleanBinarySyntaxTree(
                left:       left,
                right:      right,
                operation:  this.Operation
            );
        }
    }
}