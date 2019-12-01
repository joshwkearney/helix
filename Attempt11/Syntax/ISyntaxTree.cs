using System;
using System.Text;

namespace Attempt10 {
    public interface ISyntaxTree {
        ITrophyType ExpressionType { get; }

        void Accept(ISyntaxTreeVisitor visitor);
    }

    public interface ISyntaxTreeVisitor {
        void Visit(BinaryExpressionSyntax value);
        void Visit(UnaryExpressionSyntax value);
        void Visit(Int64LiteralSyntax value);
        void Visit(VariableLiteralSyntax value);
        void Visit(IfExpressionSyntax value);
        void Visit(VariableDefinitionSyntax value);
        void Visit(FunctionLiteralSyntax value);
        void Visit(FunctionInvokeSyntax value);
        void Visit(BoolLiteralSyntax value);
        void Visit(Real64Literal value);
    }
}