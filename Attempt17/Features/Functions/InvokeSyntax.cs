using Attempt17.TypeChecking;
using System.Collections.Immutable;

namespace Attempt17.Features.Functions {
    public class InvokeSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public FunctionInfo Target { get; }

        public ImmutableList<ISyntax<TypeCheckTag>> Arguments { get; }

        public InvokeSyntax(TypeCheckTag tag, FunctionInfo target, ImmutableList<ISyntax<TypeCheckTag>> arguments) {
            this.Tag = tag;
            this.Target = target;
            this.Arguments = arguments;
        }
    }
}