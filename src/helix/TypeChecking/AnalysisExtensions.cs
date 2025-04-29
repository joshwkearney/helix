using Helix.Syntax.TypedTree;
using Helix.Types;

namespace Helix.TypeChecking {
    public static class AnalysisExtensions {
        public static bool TryResolvePath(this TypeFrame types, IdentifierPath scope, string name, out IdentifierPath path) {
            while (true) {
                path = scope.Append(name);
                if (types.Declarations.ContainsKey(path)) {
                    return true;
                }

                if (scope.Segments.Any()) {
                    scope = scope.Pop();
                }
                else {
                    return false;
                }
            }
        }

        public static IdentifierPath ResolvePath(this TypeFrame types, IdentifierPath scope, string name) {
            if (types.TryResolvePath(scope, name, out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{name}' does not contain a value.");
        }

        public static bool TryResolveName(this TypeFrame types, IdentifierPath scope, string name, out NominalType value) {
            if (!types.TryResolvePath(scope, name, out var path)) {
                value = null;
                return false;
            }

            if (!types.Declarations.TryGetValue(path, out value)) {
                value = null;
                return false;
            }

            return true;
        }

        public static bool TryGetFunction(this TypeFrame types, IdentifierPath path, out FunctionType type) {
            return types.Declarations
                .GetValueOrNone(path)
                .SelectMany(x => x.AsFunction(types))
                .TryGetValue(out type);
        }

        public static PointerType AssertIsPointer(this ITypedExpression parse, TypeFrame types) {
            var type = parse.ReturnType;

            if (!type.AsVariable(types).TryGetValue(out var pointer)) {
                throw TypeException.ExpectedVariableType(parse.Location, type);
            }

            return pointer;
        }

        public static bool TryGetVariable(this TypeFrame types, IdentifierPath path, out PointerType type) {
            return types.Declarations
                .GetValueOrNone(path)
                .SelectMany(x => x.AsVariable(types))
                .TryGetValue(out type);
        }
        
        public static IEnumerable<KeyValuePair<IdentifierPath, HelixType>> GetMembers(this HelixType type, TypeFrame types) {
            var dict = new Dictionary<IdentifierPath, HelixType>();

            foreach (var (memPath, memType) in GetMemberPaths(type, types)) {
                dict[memPath] = memType;
            }

            return dict;
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(
            HelixType type,
            TypeFrame types) {

            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath,
            HelixType type,
            TypeFrame types) {

            yield return (basePath, type);

            if (!type.AsStruct(types).TryGetValue(out var structSig)) {
                yield break;
            }

            foreach (var mem in structSig.Members) {
                var path = basePath.Append(mem.Name);

                foreach (var subs in GetMemberPathsHelper(path, mem.Type, types)) {
                    yield return subs;
                }
            }
        }
    }
}
