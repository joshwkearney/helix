using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Functions {
    public class FunctionLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public FunctionLiteralSyntax(T tag) {
            this.Tag = tag;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.FunctionsVisitor.VisitFunctionLiteral(this, visitor, context);
        }
    }
}