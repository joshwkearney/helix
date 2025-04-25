using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Analysis {
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

        public static bool TryResolveName(this TypeFrame types, IdentifierPath scope, string name, out HelixType value) {
            if (!types.TryResolvePath(scope, name, out var path)) {
                value = null;
                return false;
            }

            if (!types.Declarations.TryGetValue(path, out var info)) {
                value = null;
                return false;
            }

            value = info.Type;
            return true;
        }

        public static bool TryGetFunction(this TypeFrame types, IdentifierPath path, out FunctionType type) {
            return types.Declarations
                .GetValueOrNone(path)
                .SelectMany(x => x.Type.AsFunction(types))
                .TryGetValue(out type);
        }

        public static Option<PointerType> AsVariable(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) is PointerType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<FunctionType> AsFunction(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) is FunctionType funcSig) {
                return funcSig;
            }
            else {
                return Option.None;
            }            
        }

        public static Option<StructType> AsStruct(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) is StructType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<UnionType> AsUnion(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) is UnionType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<ArrayType> AsArray(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) is ArrayType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static bool IsBool(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) == PrimitiveType.Bool) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsInt(this HelixType type, TypeFrame types) {
            if (type.GetSignatureSupertype(types) == PrimitiveType.Word) {
                return true;
            }
            else {
                return false;
            }
        }

        public static PointerType AssertIsPointer(this ISyntax parse, TypeFrame types) {
            var type = parse.ReturnType;

            if (!type.AsVariable(types).TryGetValue(out var pointer)) {
                throw TypeException.ExpectedVariableType(parse.Location, type);
            }

            return pointer;
        }

        public static bool TryGetVariable(this TypeFrame types, IdentifierPath path, out PointerType type) {
            return types.Declarations
                .GetValueOrNone(path)
                .SelectMany(x => x.Type.AsVariable(types))
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
