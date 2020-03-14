using System.Collections.Immutable;
using Attempt17.Parsing;

namespace Attempt17.Features.Containers {
    public class MemberUsageSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ImmutableList<IMemberUsageSegment> UsageSegments { get; }

        public MemberUsageSyntax(T tag, ISyntax<T> target, ImmutableList<IMemberUsageSegment> segs) {
            this.Tag = tag;
            this.Target = target;
            this.UsageSegments = segs;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.ContainersVisitor.VisitMemberUsage(this, visitor, context);
        }
    }
}