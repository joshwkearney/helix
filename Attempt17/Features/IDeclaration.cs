using System;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features  {
    public interface IDeclaration<TTag> : ISyntax<TTag> {
        public T Accept<T>(IDeclarationVisitor<T, TTag> visitor, ITypeCheckScope scope);
    }
}