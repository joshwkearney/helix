using System;
namespace Attempt17.Features.Variables {
    public interface IVariablesVisitor<T, TTag, TContext> {
        public T VisitMove(MoveSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);

        public T VisitStore(StoreSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);

        public T VisitVariableParseAccess(VariableAccessParseSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitVariableAccess(VariableAccessSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitVariableInit(VariableInitSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}
