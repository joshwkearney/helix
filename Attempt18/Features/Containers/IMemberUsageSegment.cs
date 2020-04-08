using System;

namespace Attempt18.Features.Containers {
    public interface IMemberUsageSegment {
        public IMemberAccessTarget Apply(IMemberAccessTarget target);
    }
}