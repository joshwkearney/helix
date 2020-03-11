using System;
using System.Collections.Immutable;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Parsing {
    public class ParseCompositeDeclaration : IParseDeclaration {
        public ParseTag Tag { get; }

        public CompositeInfo CompositeInfo { get; }

        public ImmutableList<IParseDeclaration> Declarations { get; }

        public ParseCompositeDeclaration(ParseTag tag, CompositeInfo info, ImmutableList<IParseDeclaration> decls) {
            this.Tag = tag;
            this.CompositeInfo = info;
            this.Declarations = decls;
        }

        T IParseDeclaration.Accept<T>(IParseDeclarationVisitor<T> visitor) {
            return visitor.VisitCompositeDeclaration(this);
        }
    }
}