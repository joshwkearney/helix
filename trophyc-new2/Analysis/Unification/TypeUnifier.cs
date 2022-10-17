using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        public static Option<ISyntaxTree> TryUnifyTo(this ITypesRecorder types, ISyntaxTree fromTree, 
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

        private static Option<Func<ISyntaxTree, ISyntaxTree>> TryUnifyToHelper(TrophyType fromType, TrophyType toType) {

            if (fromType == toType) {
                return Option.Some<Func<ISyntaxTree, ISyntaxTree>>(x => x);
            }

            var result = TryUnifyToPrimitives(fromType, toType);
            if (result != null) {
                return Option.Some(result);
            }

            return Option.None;
        }
    }
}
