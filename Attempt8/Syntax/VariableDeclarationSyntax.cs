using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.Analyzing {
    public class VariableDeclarationSyntax : ISyntax {
        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol => this.AssignmentExpression.TypeSymbol;

        public ISyntax AssignmentExpression { get; }

        public VariableLocation Variable { get; }

        public VariableDeclarationSyntax(ISyntax assign, VariableLocation variable, AnalyticScope scope) {
            this.AssignmentExpression = assign;
            this.Scope = scope;
            this.Variable = variable;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}