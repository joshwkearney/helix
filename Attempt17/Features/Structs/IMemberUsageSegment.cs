using System;

namespace Attempt17.Features.Structs {
    public interface IMemberUsageSegment {
        public T Match<T>(Func<MemberAccessSegment, T> ifAccess, Func<MemberInvokeSegment, T> ifInvoke);
    }
}