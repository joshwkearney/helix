using System;
using System.Collections.Immutable;
using Attempt17.Parsing;

namespace Attempt17.Features.Structs {
    public class MemberInvokeSegment : IMemberUsageSegment {
        public string MemberName { get; }

        public ImmutableList<ISyntax<ParseTag>> Arguments { get; }

        public MemberInvokeSegment(string name, ImmutableList<ISyntax<ParseTag>> args) {
            this.MemberName = name;
            this.Arguments = args;
        }

        public T Match<T>(Func<MemberAccessSegment, T> ifAccess, Func<MemberInvokeSegment, T> ifInvoke) {
            return ifInvoke(this);
        }
    }
}