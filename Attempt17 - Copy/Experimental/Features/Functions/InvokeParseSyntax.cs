using System.Collections.Immutable;

namespace Attempt17.Experimental.Features.Functions {
    public class InvokeParseSyntax : ISyntax<ParseInfo> {
        public ParseInfo Tag { get; }

        public ISyntax<ParseInfo> Target { get; }

        public ImmutableList<ISyntax<ParseInfo>> Arguments { get; }

        public InvokeParseSyntax(ParseInfo tag, ISyntax<ParseInfo> target, ImmutableList<ISyntax<ParseInfo>> arguments) {
            this.Tag = tag;
            this.Target = target;
            this.Arguments = arguments;
        }
    }
}