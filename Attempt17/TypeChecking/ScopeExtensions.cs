using System.Collections.Generic;

namespace Attempt17.TypeChecking {
    public static class ScopeExtensions {
        public static IOption<VariableInfo> FindVariable(this IScope scope, string name) {
            foreach (var path in GetPossiblePaths(scope.Path, name)) {
                if (scope.FindVariable(path).TryGetValue(out var info)) {
                    return Option.Some(info);
                } 
            }

            return Option.None<VariableInfo>();
        }

        public static IOption<FunctionInfo> FindFunction(this IScope scope, string name) {
            foreach (var path in GetPossiblePaths(scope.Path, name)) {
                if (scope.FindFunction(path).TryGetValue(out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<FunctionInfo>();
        }

        public static bool IsNameTaken(this IScope scope, string name) {
            return scope.FindVariable(name).Any() || scope.FindFunction(name).Any();
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