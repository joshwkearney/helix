using System.Collections.Immutable;
using Attempt17.Parsing;

namespace Attempt17.Features.Structs {
    public class MemberUsageParseSyntax : ISyntax<ParseTag> {
        public ParseTag Tag { get; }

        public ISyntax<ParseTag> Target { get; }

        public ImmutableList<IMemberUsageSegment> UsageSegments { get; }

        public MemberUsageParseSyntax(ParseTag tag, ISyntax<ParseTag> target, ImmutableList<IMemberUsageSegment> segs) {
            this.Tag = tag;
            this.Target = target;
            this.UsageSegments = segs;
        }
    }
}