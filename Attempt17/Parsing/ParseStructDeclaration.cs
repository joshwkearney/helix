using System;
using System.Collections.Immutable;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Parsing {
    public class ParseStructDeclaration : IParseDeclaration {
        public ParseTag Tag { get; }

        public StructInfo StructInfo { get; }

        public ImmutableList<IParseDeclaration> Declarations { get; }

        public ParseStructDeclaration(ParseTag tag, StructInfo info, ImmutableList<IParseDeclaration> decls) {
            this.Tag = tag;
            this.StructInfo = info;
            this.Declarations = decls;
        }

        T IParseDeclaration.Accept<T>(IParseDeclarationVisitor<T> visitor) {
            return visitor.VisitStructDeclaration(this);
        }
    }
}