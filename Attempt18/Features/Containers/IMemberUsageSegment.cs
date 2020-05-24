using System;

namespace Attempt19.Features.Containers {
    public interface IMemberUsageSegment {
        public IMemberAccessTarget Apply(IMemberAccessTarget target);
    }
}