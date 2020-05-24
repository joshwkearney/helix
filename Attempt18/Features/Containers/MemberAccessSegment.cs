using System;

namespace Attempt19.Features.Containers {
    public class MemberAccessSegment : IMemberUsageSegment {
        public string MemberName { get; }

        public MemberAccessSegment(string name) {
            this.MemberName = name;
        }

        public IMemberAccessTarget Apply(IMemberAccessTarget target) {
            return target.AccessMember(this.MemberName);
        }
    }
}