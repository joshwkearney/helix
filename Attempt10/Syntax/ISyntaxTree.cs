using System;
using System.Text;

namespace Attempt12 {
    public interface ISyntaxTree {
        ITrophyType ExpressionType { get; }
        Scope Scope { get; }
        bool IsConstant { get; }

        void Accept(ISyntaxTreeVisitor visitor);
    }

    public interface ISyntaxTreeVisitor {
        void Visit(Int64LiteralSyntax value);
        void Visit(VariableLiteralSyntax value);
        void Visit(IfExpressionSyntax value);
        void Visit(VariableDefinitionSyntax value);
        void Visit(FunctionLiteralSyntax value);
        void Visit(FunctionInvokeSyntax value);
        void Visit(BoolLiteralSyntax value);
        void Visit(Real64Literal value);
        void Visit(PrimitiveOperationSyntax value);
        void Visit(FunctionRecurseLiteral value);
    }
}