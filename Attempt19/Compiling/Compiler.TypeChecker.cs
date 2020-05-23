using System;
using Attempt18.Features;
using Attempt18.Parsing;
using Attempt18.TypeChecking;
using Attempt18.Types;

namespace Attempt18.Compiling {
    public partial class Compiler {
        private class TypeChecker : ITypeChecker {
            public IOption<ISyntax<TypeCheckTag>> Unify(ISyntax<TypeCheckTag> syntax,
                ITypeCheckScope scope, LanguageType type) {

                throw new NotImplementedException();
            }
        }
    }
}