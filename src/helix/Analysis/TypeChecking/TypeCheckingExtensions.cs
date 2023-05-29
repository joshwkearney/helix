using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.Predicates;

namespace Helix.Analysis {
    public static partial class TypeCheckingExtensions {
        public static PointerType AssertIsPointer(this ISyntaxTree syntax, TypeFrame types) {
            var type = syntax.GetReturnType(types);

            if (type is not PointerType pointer) {
                throw TypeException.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntaxTree WithMutationType(this ISyntaxTree syntax, TypeFrame types) {
            var betterType = syntax.GetReturnType(types).GetMutationSupertype(types);

            return syntax.UnifyTo(betterType, types);
        }

        public static bool TryGetVariable(this TypeFrame types, IdentifierPath path, out PointerType type) {
            return types.SyntaxValues
                .GetValueOrNone(path)
                .SelectMany(x => x.AsType(types))
                .SelectMany(x => x.AsVariable(types))
                .TryGetValue(out type);
        }

        public static void SetReturnType(this ISyntaxTree syntax, HelixType type, TypeFrame types) {
            types.ReturnTypes[syntax] = type;
        }

        public static void SetCapturedVariables(this ISyntaxTree syntax, TypeFrame types) {
            types.CapturedVariables[syntax] = Array.Empty<VariableCapture>();
        }

        public static void SetCapturedVariables(this ISyntaxTree syntax, ISyntaxTree child, TypeFrame types) {
            types.CapturedVariables[syntax] = child.GetCapturedVariables(types);
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            ISyntaxTree child1,
            ISyntaxTree child2,
            TypeFrame types) {

            var caps = child1.GetCapturedVariables(types)
                .Concat(child2.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            ISyntaxTree child1,
            ISyntaxTree child2,
            ISyntaxTree child3,
            TypeFrame types) {

            var caps = child1.GetCapturedVariables(types)
                .Concat(child2.GetCapturedVariables(types))
                .Concat(child3.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IEnumerable<ISyntaxTree> children,
            TypeFrame types) {

            var caps = children
                .SelectMany(x => x.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IdentifierPath variable,
            VariableCaptureKind kind,
            PointerType sig,
            TypeFrame types) {

            types.CapturedVariables[syntax] = new[] { new VariableCapture(variable, kind, sig) };
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IEnumerable<VariableCapture> caps,
            TypeFrame types) {

            types.CapturedVariables[syntax] = caps.ToArray();
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
