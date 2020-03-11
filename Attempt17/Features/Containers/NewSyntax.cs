using System;
using System.Collections.Immutable;
using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.Features.Containers {
    public class MemberInstantiation<T> {
        public string MemberName { get; }

        public ISyntax<T> Value { get; }

        public MemberInstantiation(string name, ISyntax<T> value) {
            this.MemberName = name;
            this.Value = value;
        }
    }

    public class NewSyntax : ISyntax<ParseTag> {
        public ParseTag Tag { get; }

        public LanguageType Type { get; }

        public ImmutableList<MemberInstantiation<ParseTag>> Instantiations { get; }

        public NewSyntax(ParseTag tag, LanguageType type, ImmutableList<MemberInstantiation<ParseTag>> insts) {
            this.Tag = tag;
            this.Type = type;
            this.Instantiations = insts;
        }
    }
}