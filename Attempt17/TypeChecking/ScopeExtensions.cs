using System.Collections.Generic;

namespace Attempt17.TypeChecking {
    public static class ScopeExtensions {
        public static IOption<VariableInfo> FindVariable(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).SelectMany(x => x.AsVariableInfo());
        }

        public static IOption<FunctionInfo> FindFunction(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).SelectMany(x => x.AsFunctionInfo());
        }

        public static IOption<StructInfo> FindStruct(this ITypeCheckScope scope, IdentifierPath path) {
            return scope.FindTypeInfo(path).SelectMany(x => x.AsStructInfo());
        }

        public static IOption<TypeInfo> FindTypeInfo(this ITypeCheckScope scope, string name) {
            foreach (var path in GetPossiblePaths(scope.Path, name)) {
                if (scope.FindTypeInfo(path).TryGetValue(out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<VariableInfo>();
        }

        public static IOption<VariableInfo> FindVariable(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).SelectMany(x => x.AsVariableInfo());
        }

        public static IOption<FunctionInfo> FindFunction(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).SelectMany(x => x.AsFunctionInfo());
        }

        public static IOption<StructInfo> FindStruct(this ITypeCheckScope scope, string name) {
            return scope.FindTypeInfo(name).SelectMany(x => x.AsStructInfo());
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