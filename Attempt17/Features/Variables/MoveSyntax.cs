using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Variables {
    public enum MovementKind {
        ValueMove, LiteralMove
    }

    public class MoveSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public MovementKind Kind { get; }

        public string VariableName { get; }

        public MoveSyntax(T tag, MovementKind kind, string variableName) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableName = variableName;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.VariablesVisitor.VisitMove(this, visitor, context);
        }
    }
}