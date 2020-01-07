using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Experimental.Features.Functions {
    public class InvokeSyntax : ISyntax<TypeCheckInfo> {
        public TypeCheckInfo Tag { get; }

        public FunctionInfo Target { get; }

        public ImmutableList<ISyntax<TypeCheckInfo>> Arguments { get; }

        public InvokeSyntax(TypeCheckInfo tag, FunctionInfo target, ImmutableList<ISyntax<TypeCheckInfo>> arguments) {
            this.Tag = tag;
            this.Target = target;
            this.Arguments = arguments;
        }
    }
}