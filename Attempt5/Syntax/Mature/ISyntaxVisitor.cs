using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public interface ISyntaxVisitor {
        void Visit(Int32Literal syntax);
        void Visit(FunctionCallExpression syntax);
        void Visit(VariableAssignment syntax);
        void Visit(VariableLocation syntax);
        void Visit(Statement syntax);
    }
}