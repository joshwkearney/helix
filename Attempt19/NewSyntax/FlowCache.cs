using System;
using Attempt18;

namespace Attempt17.NewSyntax {
    public class FlowCache {
        public ImmutableGraph<IdentifierPath> DependentVariables { get; set; }

        public ImmutableGraph<IdentifierPath> CapturedVariables { get; set; }
    }
}
