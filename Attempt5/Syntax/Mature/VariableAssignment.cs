using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class VariableAssignment : ISyntax {
        public VariableLocation Variable { get; }

        public ISyntax AssignExpression { get; }

        public ISyntax ScopeExpression { get; }

        public ILanguageType ExpressionType => this.AssignExpression.ExpressionType;

        public bool IsReadOnly { get; }

        public VariableAssignment(VariableLocation loc, ISyntax assign, ISyntax scope, bool isReadOnly) {
            this.Variable = loc;
            this.AssignExpression = assign;
            this.ScopeExpression = scope;
            this.IsReadOnly = isReadOnly;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}