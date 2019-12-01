using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt3 {
    public interface ISyntaxVisitor {
        void Visit(IdentifierLiteral leaf);
        void Visit(IntegerLiteral leaf);
        void Visit(FunctionCallExpression expr);
        void Visit(FunctionLiteral expr);
    }
}