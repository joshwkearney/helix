using Trophy.Analysis.Types;
using Trophy.Features.Aggregates;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public static partial class AnalysisExtensions {
        public static PointerType AssertIsPointer(this ISyntax syntax, ITypesRecorder types) {
            var type = types.GetReturnType(syntax);

            if (type is not PointerType pointer) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntax WithMutableType(this ISyntax syntax, ITypesRecorder types) {
            var type = types.GetReturnType(syntax);
            var betterType = type.ToMutableType();

            return syntax.UnifyTo(betterType, types);
        }

        public static ISyntax UnifyTo(this ISyntax fromSyntax, TrophyType toType, ITypesRecorder types) {
            var type = types.GetReturnType(fromSyntax);

            if (!type.CanUnifyTo(toType, types, false)) {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, toType, type);
            }

            var result = type.UnifyTo(toType, fromSyntax, false, types).CheckTypes(types);

            types.SetReturnType(result, toType);
            return result;
        }

        public static ISyntax UnifyFrom(this ISyntax fromSyntax, ISyntax otherSyntax, ITypesRecorder types) {
            var type1 = types.GetReturnType(fromSyntax);
            var type2 = types.GetReturnType(otherSyntax);

            if (type1.CanUnifyFrom(type2, types)) {
                return fromSyntax.UnifyTo(type1.UnifyFrom(type2, types), types);
            }
            else if (type2.CanUnifyFrom(type1, types)) {
                return fromSyntax.UnifyTo(type2.UnifyFrom(type1, types), types);
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, type1, type2);
            }
        }
    }
}
