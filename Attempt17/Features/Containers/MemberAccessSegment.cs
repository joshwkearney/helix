using System;

namespace Attempt17.Features.Containers {
    public class MemberAccessSegment : IMemberUsageSegment {
        public string MemberName { get; }

        public MemberAccessSegment(string name) {
            this.MemberName = name;
        }

        public T Match<T>(Func<MemberAccessSegment, T> ifAccess, Func<MemberInvokeSegment, T> ifInvoke) {
            return ifAccess(this);
        }
    }
}