using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public static partial class TypeCheckingExtensions {
        public static PointerType AssertIsPointer(this ISyntaxTree syntax, ITypedFrame types) {
            var type = syntax.GetReturnType(types);

            if (type is not PointerType pointer) {
                throw TypeException.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntaxTree WithMutableType(this ISyntaxTree syntax, TypeFrame types) {
            var betterType = syntax.GetReturnType(types).GetNaturalSupertype(types);

            return syntax.UnifyTo(betterType, types);
        }
    }
}
