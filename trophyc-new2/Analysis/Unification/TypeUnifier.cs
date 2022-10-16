using Trophy.Parsing;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        public static Option<ISyntaxTree> TryUnifyTo(ISyntaxTree fromTree, TrophyType fromType, TrophyType toType) {
            return TryUnifyToHelper(fromType, toType).Select(x => x(fromTree));
        }

        public static Option<TrophyType> TryUnifyFrom(TrophyType type1, TrophyType type2) {
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
