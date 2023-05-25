using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.Predicates;

namespace Helix.Analysis
{
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

        public static ISyntaxPredicate GetPredicate(this ISyntaxTree syntax, TypeFrame types) {
            return types.Predicates[syntax];
        }

        public static void SetPredicate(this ISyntaxTree syntax, TypeFrame types) {
            types.Predicates[syntax] = ISyntaxPredicate.Empty;
        }

        public static void SetPredicate(this ISyntaxTree syntax, ISyntaxPredicate predicate, TypeFrame types) {
            types.Predicates[syntax] = predicate;
        }

        public static void SetPredicate(
            this ISyntaxTree syntax,
            ISyntaxTree child1,
            TypeFrame types) {

            types.Predicates[syntax] = child1.GetPredicate(types);
        }

        public static void SetPredicate(
            this ISyntaxTree syntax,
            ISyntaxTree child1,
            ISyntaxTree child2,
            TypeFrame types) {

            types.Predicates[syntax] = child1.GetPredicate(types).And(child2.GetPredicate(types));
        }

        public static void SetPredicate(
            this ISyntaxTree syntax,
            ISyntaxTree child1,
            ISyntaxTree child2,
            ISyntaxTree child3,
            TypeFrame types) {

            types.Predicates[syntax] = child1.GetPredicate(types)
                .And(child2.GetPredicate(types))
                .And(child3.GetPredicate(types));
        }

        public static void SetPredicate(
            this ISyntaxTree syntax,
            IEnumerable<ISyntaxTree> children,
            TypeFrame types) {

            var predicate = children
                .Select(x => x.GetPredicate(types))
                .Aggregate((x, y) => x.And(y));

            types.Predicates[syntax] = predicate;
        }
    }
}
