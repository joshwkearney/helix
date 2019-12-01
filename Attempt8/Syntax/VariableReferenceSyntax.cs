using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.Analyzing {
    public class VariableReferenceSyntax : ISyntax {
        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol => this.Variable.Type;

        public VariableLocation Variable { get; }

        public VariableReferenceSyntax(VariableLocation variable, AnalyticScope scope) {
            this.Scope = scope;
            this.Variable = variable;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}