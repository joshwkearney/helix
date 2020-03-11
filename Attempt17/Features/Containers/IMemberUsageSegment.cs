using System;

namespace Attempt17.Features.Containers {
    public interface IMemberUsageSegment {
        public T Match<T>(Func<MemberAccessSegment, T> ifAccess, Func<MemberInvokeSegment, T> ifInvoke);
    }
}