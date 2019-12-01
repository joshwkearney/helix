using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Parsing {
    public interface ISyntaxVisitor {
        void VisitInt32Literal(Int32Literal literal);
        void VisitUnaryExpression(UnaryExpression expr);
        void VisitBinaryExpression(BinaryExpression expr);
        void VisitVariableDeclaration(VariableDeclaration decl);
        void VisitVariableUsage(VariableUsage usage);
        void VisitBoolLiteral(BoolLiteral literal);
        void VisitIfExpression(IfExpression expr);
    }
}