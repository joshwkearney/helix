using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.BinaryExpression {
    public class BinaryExpressionParseTree : IParseTree {
        public BinaryExpressionKind Kind { get; }

        public IParseTree Left { get; }
        
        public IParseTree Right { get; }

        public TokenLocation Location { get; }

        public BinaryExpressionParseTree(TokenLocation location, BinaryExpressionKind kind, IParseTree left, IParseTree right) {
            this.Location = location;
            this.Kind = kind;
            this.Left = left;
            this.Right = right;
        }

        public ISyntaxTree Analyze(Scope scope) {
            var left = this.Left.Analyze(scope);
            var right = this.Right.Analyze(scope);

            if (left.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(this.Left.Location, IntType.Instance, left.ReturnType);
            }

            if (right.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(this.Right.Location, IntType.Instance, right.ReturnType);
            }

            return new BinaryExpressionSyntaxTree(
                IntType.Instance,
                this.Kind,
                left,
                right);
        }
    }
}