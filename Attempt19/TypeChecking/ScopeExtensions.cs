using System.Collections.Generic;
using System.Linq;
using Attempt18.Types;

namespace Attempt18.TypeChecking {
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
            return scope.FindTypeInfo(new IdentifierPath(name));
        }

        public static IOption<IIdentifierTarget> FindTypeInfo(this ITypeCheckScope scope, IdentifierPath name) {
            var scopePath = scope.Path.Append(name).Pop();

            foreach (var path in GetPossiblePaths(scopePath, name.Segments.Last())) {
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