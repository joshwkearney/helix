using System;
namespace Attempt17.Features.Variables {
    public class VariableAccessSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public VariableInfo VariableInfo { get; }

        public VariableAccessKind Kind { get; }

        public VariableAccessSyntax(T tag, VariableAccessKind kind, VariableInfo info) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableInfo = info;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.VariablesVisitor.VisitVariableAccess(this, visitor, context);
        }
    }

    public class VariableAccessParseSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public string VariableName { get; }

        public VariableAccessKind Kind { get; }

        public VariableAccessParseSyntax(T tag, VariableAccessKind kind, string name) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableName = name;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.VariablesVisitor.VisitVariableParseAccess(this, visitor, context);
        }
    }
}
