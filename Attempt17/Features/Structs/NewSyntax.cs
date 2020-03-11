using System;
using System.Collections.Immutable;
using Attempt17.Types;

namespace Attempt17.Features.Structs {
    public class MemberInstantiation<T> {
        public string MemberName { get; }

        public ISyntax<T> Value { get; }

        public MemberInstantiation(string name, ISyntax<T> value) {
            this.MemberName = name;
            this.Value = value;
        }
    }

    public class NewSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public LanguageType Type { get; }

        public ImmutableList<MemberInstantiation<T>> Instantiations { get; }

        public NewSyntax(T tag, LanguageType type, ImmutableList<MemberInstantiation<T>> insts) {
            this.Tag = tag;
            this.Type = type;
            this.Instantiations = insts;
        }
    }
}