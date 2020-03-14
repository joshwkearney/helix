using System;
namespace Attempt17.Features.Functions {
    public interface IFunctionsVisitor<T, TTag, TContext> {
        public T VisitFunctionDeclaration(FunctionDeclarationSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitFunctionLiteral(FunctionLiteralSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitInvoke(InvokeSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}
