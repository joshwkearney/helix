using Attempt17.Parsing;
using System.Collections.Immutable;

namespace Attempt17.Features.Functions {
    public class InvokeParseSyntax : ISyntax<ParseTag> {
        public ParseTag Tag { get; }

        public ISyntax<ParseTag> Target { get; }

        public ImmutableList<ISyntax<ParseTag>> Arguments { get; }

        public InvokeParseSyntax(ParseTag tag, ISyntax<ParseTag> target, ImmutableList<ISyntax<ParseTag>> arguments) {
            this.Tag = tag;
            this.Target = target;
            this.Arguments = arguments;
        }
    }
}