using System;
using Attempt17.Features.Containers.Arrays;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Containers.Unions;

namespace Attempt17.Features.Containers {
    public interface IContainersVisitor<T, TTag, TContext> {
        public IArraysVisitor<T, TTag, TContext> ArraysVisitor { get; }

        public ICompositesVisitor<T, TTag, TContext> CompositesVisitor { get; }

        public IUnionVisitor<T, TTag, TContext> UnionVisitor { get; }

        public T VisitNew(NewSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);

        public T VisitMemberUsage(MemberUsageSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);
    }
}