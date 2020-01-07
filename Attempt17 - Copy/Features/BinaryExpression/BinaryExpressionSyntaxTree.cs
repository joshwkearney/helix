using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.BinaryExpression {
    public class BinaryExpressionSyntaxTree : ISyntaxTree {
        public BinaryExpressionKind Kind { get; }

        public ISyntaxTree Left { get; }

        public ISyntaxTree Right { get; }

        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public BinaryExpressionSyntaxTree(LanguageType returnType, BinaryExpressionKind kind, ISyntaxTree left, ISyntaxTree right) {
            this.ReturnType = returnType;
            this.Kind = kind;
            this.Left = left;
            this.Right = right;
            this.CapturedVariables = this.Left.CapturedVariables.Union(this.Right.CapturedVariables);
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            var op = this.Kind switch {
                BinaryExpressionKind.Add => " + ",
                BinaryExpressionKind.Subtract => " - ",
                _ => throw new Exception("This should never happen"),
            };

            var left = this.Left.GenerateCode(gen);
            var right = this.Right.GenerateCode(gen);

            return left.Combine(right, (x, y) => "(" + x + op + y + ")");
        }

        public Scope ModifyLateralScope(Scope scope) => scope;
    }
}