using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Analysis {
    public static class AnalysisExtensions {
        public static bool TryResolvePath(this ITypeContext types, IdentifierPath scope, string name, out IdentifierPath path) {
            while (true) {
                path = scope.Append(name);
                if (types.GlobalSyntaxValues.ContainsKey(path)) {
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

        public static IdentifierPath ResolvePath(this ITypeContext types, IdentifierPath scope, string name) {
            if (types.TryResolvePath(scope, name, out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{name}' does not contain a value.");
        }

        public static bool TryResolveName(this ITypeContext types, IdentifierPath scope, string name, out ISyntaxTree value) {
            if (!types.TryResolvePath(scope, name, out var path)) {
                value = null;
                return false;
            }

            return types.GlobalSyntaxValues.TryGetValue(path, out value);
        }

        public static ISyntaxTree ResolveName(this ITypeContext types, IdentifierPath scope, string name) {
            return types.GlobalSyntaxValues[types.ResolvePath(scope, name)];
        }

        public static bool TryGetFunction(this ITypeContext types, IdentifierPath path, out FunctionType type) {
            return types.GlobalNominalSignatures
                .GetValueOrNone(path)
                .SelectMany(x => x.AsFunction(types))
                .TryGetValue(out type);
        }

        public static Option<PointerType> AsVariable(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) is PointerType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<FunctionType> AsFunction(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) is FunctionType funcSig) {
                return funcSig;
            }
            else {
                return Option.None;
            }            
        }

        public static Option<StructType> AsStruct(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) is StructType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<UnionType> AsUnion(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) is UnionType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<ArrayType> AsArray(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) is ArrayType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static bool IsBool(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) == PrimitiveType.Bool) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsInt(this HelixType type, ITypeContext types) {
            if (type.GetSignatureSupertype(types) == PrimitiveType.Int) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsTypeChecked(this ISyntaxTree syntax, ITypeContext types) {
            return types.ReturnTypes.ContainsKey(syntax);
        }

        public static HelixType GetReturnType(this ISyntaxTree syntax, ITypeContext types) {
            return types.ReturnTypes[syntax];
        }

        public static IReadOnlyList<VariableCapture> GetCapturedVariables(this ISyntaxTree syntax, ITypeContext types) {
            return types.CapturedVariables[syntax];
        }

        public static Bundle<HelixType> GetMembers(this HelixType type, ITypeContext types) {
            var dict = new Dictionary<IdentifierPath, HelixType>();

            foreach (var (memPath, memType) in GetMemberPaths(type, types)) {
                dict[memPath] = memType;
            }

            return new Bundle<HelixType>(dict);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(
            HelixType type,
            ITypeContext types) {

            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath,
            HelixType type,
            ITypeContext types) {

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
