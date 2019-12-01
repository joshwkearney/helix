using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.Analyzing {
    public class StatementSyntax : ISyntax {
        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol => this.ReturnExpression.TypeSymbol;

        public ISyntax StatementExpression { get; }

        public ISyntax ReturnExpression { get; }

        public StatementSyntax(ISyntax statExpr, ISyntax retExpr, AnalyticScope scope) {
            this.StatementExpression = statExpr;
            this.ReturnExpression = retExpr;
            this.Scope = scope;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}