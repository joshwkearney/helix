using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        public static Option<ISyntax> TryUnifyTo(this ITypesRecorder types, ISyntax fromTree, 
                                                     TrophyType fromType, TrophyType toType) {

            if (TryUnifyToHelper(fromType, toType).TryGetValue(out var func)) {
                var newTree = func(fromTree);

                types.SetReturnType(newTree, toType);
                return Option.Some(newTree);
            }

            return Option.None;
        }

        public static Option<TrophyType> TryUnifyFrom(this ITypesRecorder types, TrophyType type1, 
                                                      TrophyType type2) {

            if (TryUnifyToHelper(type1, type2).HasValue) {
                return type2;
            }
            else if (TryUnifyToHelper(type2, type1).HasValue) {
                return type1;
            }

            return Option.None;
        }

        private static Option<Func<ISyntax, ISyntax>> TryUnifyToHelper(TrophyType fromType, TrophyType toType) {

            if (fromType == toType) {
                return Option.Some<Func<ISyntax, ISyntax>>(x => x);
            }

            var result = TryUnifyToPrimitives(fromType, toType);
            if (result != null) {
                return Option.Some(result);
            }

            return Option.None;
        }
    }
}
