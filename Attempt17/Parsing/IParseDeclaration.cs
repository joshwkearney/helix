using System;
using System.Collections.Immutable;
using Attempt17.Parsing;
using Attempt17.Types;

namespace Attempt17.Parsing {
    public interface IParseDeclaration {
        public T Accept<T>(IParseDeclarationVisitor<T> visitor);
    }

    public interface IParseDeclarationVisitor<T> {
        public T VisitFunctionDeclaration(ParseFunctionDeclaration decl);

        public T VisitStructDeclaration(ParseStructDeclaration decl);
    }
}
