using System;
using System.Collections.Immutable;
using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.Features.Containers {
    public class NewSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public LanguageType Type { get; }

        public ImmutableList<MemberInstantiation<T>> Instantiations { get; }

        public NewSyntax(T tag, LanguageType type, ImmutableList<MemberInstantiation<T>> insts) {
            this.Tag = tag;
            this.Type = type;
            this.Instantiations = insts;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.ContainersVisitor.VisitNew(this, visitor, context);
        }
    }
}