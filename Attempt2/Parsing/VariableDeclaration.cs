using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Parsing {
    public class VariableDeclaration : IAST {
        public string Name { get; }

        public IAST AssignExpression { get; }

        public IAST AppendixExpression { get; }

        public VariableDeclaration(string name, IAST assignExpr, IAST appendix) {
            this.Name = name;
            this.AssignExpression = assignExpr;
            this.AppendixExpression = appendix;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitVariableDeclaration(this);
        }
    }
}