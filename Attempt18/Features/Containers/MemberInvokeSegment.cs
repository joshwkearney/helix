using System;
using System.Collections.Immutable;
using Attempt18.Parsing;

namespace Attempt18.Features.Containers {
    public class MemberInvokeSegment : IMemberUsageSegment {
        public string MemberName { get; }

        public ISyntax[] Arguments { get; }

        public MemberInvokeSegment(string name, ISyntax[] args) {
            this.MemberName = name;
            this.Arguments = args;
        }

        public IMemberAccessTarget Apply(IMemberAccessTarget target) {
            return target.InvokeMember(this.MemberName, this.Arguments);
        }
    }
}