using System;
using System.Text;

namespace Attempt9 {
    public interface IParseTree {
        void Accept(IParseTreeVisitor visitor);
    }

    public interface IParseTreeVisitor {
        void Visit(BinaryExpression value);
        void Visit(UnaryExpression value);
        void Visit(Int64Literal value);
        void Visit(VariableLiteral value);
        void Visit(IfExpression value);
        void Visit(VariableDefinition value);
    }
}