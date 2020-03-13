using System.Collections.Generic;

namespace Attempt17.TypeChecking {
    public static class ScopeExtensions {
        public static IOption<VariableInfo> FindVariable(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).SelectMany(x => x.AsVariable());
        }

        public static IOption<FunctionInfo> FindFunction(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).SelectMany(x => x.AsFunction());
        }

        public static IOption<CompositeInfo> FindComposite(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).SelectMany(x => x.AsComposite());
        }

        public static IOption<IIdentifierTarget> FindTypeInfo(this ITypeCheckScope scope, string name) {
            foreach (var path in GetPossiblePaths(scope.Path, name)) {
                if (scope.FindTypeInfo(path).TryGetValue(out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<VariableInfo>();
        }

        public static IOption<VariableInfo> FindVariable(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).SelectMany(x => x.AsVariable());
        }

        public static IOption<FunctionInfo> FindFunction(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).SelectMany(x => x.AsFunction());
        }

        public static IOption<CompositeInfo> FindComposite(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).SelectMany(x => x.AsComposite());
        }

        public static bool IsPathTaken(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).Any();
        }

        public static bool IsNameTaken(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).Any();
        }

        private static List<IdentifierPath> GetPossiblePaths(IdentifierPath basePath, string name) {
            var allPaths = new List<IdentifierPath>();

            while (true) {
                allPaths.Add(basePath.Append(name));

                if (basePath == new IdentifierPath()) {
                    break;
                }

                basePath = basePath.Pop();
            }

            return allPaths;
        }
    }
}