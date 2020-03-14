using System;
using Attempt17.Features;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public partial class Compiler {
        private class TypeChecker : ITypeChecker {
            public IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax,
                ITypeCheckScope scope, LanguageType type) {

                throw new NotImplementedException();
            }
        }
    }
}