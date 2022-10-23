using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Parsing;

namespace Helix.Analysis {
    public static partial class AnalysisExtensions {
        public static Lifetime MergeLifetimes(this IEnumerable<ISyntaxTree> tree, SyntaxFrame types) {
            return tree
                .Select(x => types.Lifetimes[x])
                .Aggregate(new Lifetime(), (x, y) => x.Merge(y));
        }

        public static PointerType AssertIsPointer(this ISyntaxTree syntax, SyntaxFrame types) {
            var type = types.ReturnTypes[syntax];

            if (type is not PointerType pointer) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntaxTree WithMutableType(this ISyntaxTree syntax, SyntaxFrame types) {
            var betterType = types.ReturnTypes[syntax].ToMutableType();

            return syntax.UnifyTo(betterType, types);
        }

        public static ISyntaxTree UnifyTo(this ISyntaxTree fromSyntax, HelixType toType, SyntaxFrame types) {
            var type = types.ReturnTypes[fromSyntax];

            if (!type.CanUnifyTo(toType, types, false)) {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, toType, type);
            }

            var result = type.UnifyTo(toType, fromSyntax, false, types).CheckTypes(types);

            types.ReturnTypes[result] = toType;
            return result;
        }

        public static ISyntaxTree UnifyFrom(this ISyntaxTree fromSyntax, ISyntaxTree otherSyntax, SyntaxFrame types) {
            var type1 = types.ReturnTypes[fromSyntax];
            var type2 = types.ReturnTypes[otherSyntax];

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
