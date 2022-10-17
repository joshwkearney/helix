using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public static class TypeCheckingExtensions {
        public static PointerType AssertIsPointer(this ITypesRecorder types, ISyntax syntax) {
            var type = types.GetReturnType(syntax);

            if (!type.AsPointerType().TryGetValue(out var pointer)) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }
    }
}
