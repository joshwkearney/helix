using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class Statement : ISyntax {
        public ISyntax StatementExpression { get; }

        public ISyntax ReturnExpression { get; }

        public ILanguageType ExpressionType => this.ReturnExpression.ExpressionType;

        public Statement(ISyntax statExpr, ISyntax retExpr) {
            this.StatementExpression = statExpr;
            this.ReturnExpression = retExpr;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}