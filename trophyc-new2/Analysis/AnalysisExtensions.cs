using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public static class AnalysisExtensions {
        public static PointerType AssertIsPointer(this ISyntax syntax, ITypesRecorder types) {
            var type = types.GetReturnType(syntax);

            if (!type.AsPointerType().TryGetValue(out var pointer)) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntax RemoveDependentTyping(this ISyntax syntax, ITypesRecorder types) {
            var type = types.GetReturnType(syntax);
            var betterType = type.RemoveDependentTyping();

            return syntax.UnifyTo(betterType, types);
        }

        public static ISyntax UnifyTo(this ISyntax fromSyntax, TrophyType toType, ITypesRecorder types) {
            var type = types.GetReturnType(fromSyntax);

            if (!type.CanUnifyTo(toType)) {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, toType, type);
            }

            var result = type.UnifyTo(toType, fromSyntax).CheckTypes(types);

            types.SetReturnType(result, toType);
            return result;
        }

        public static ISyntax UnifyFrom(this ISyntax fromSyntax, ISyntax otherSyntax, ITypesRecorder types) {
            var type1 = types.GetReturnType(fromSyntax);
            var type2 = types.GetReturnType(otherSyntax);

            if (type1.CanUnifyFrom(type2)) {
                return fromSyntax.UnifyTo(type1.UnifyFrom(type2), types);
            }
            else if (type2.CanUnifyFrom(type1)) {
                return fromSyntax.UnifyTo(type2.UnifyFrom(type1), types);
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, type1, type2);
            }
        }
    }
}
