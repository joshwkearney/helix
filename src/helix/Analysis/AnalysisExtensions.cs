using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Analysis {
    public static class AnalysisExtensions {
        public static bool TryResolvePath(this ITypedFrame types, IdentifierPath scope, string name, out IdentifierPath path) {
            while (true) {
                path = scope.Append(name);
                if (types.SyntaxValues.ContainsKey(path)) {
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

        public static IdentifierPath ResolvePath(this ITypedFrame types, IdentifierPath scope, string name) {
            if (types.TryResolvePath(scope, name, out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{name}' does not contain a value.");
        }

        public static bool TryResolveName(this ITypedFrame types, IdentifierPath scope, string name, out ISyntaxTree value) {
            if (!types.TryResolvePath(scope, name, out var path)) {
                value = null;
                return false;
            }

            return types.SyntaxValues.TryGetValue(path, out value);
        }

        public static ISyntaxTree ResolveName(this ITypedFrame types, IdentifierPath scope, string name) {
            return types.SyntaxValues[types.ResolvePath(scope, name)];
        }

        public static bool TryGetVariable(this ITypedFrame types, IdentifierPath path, out PointerType type) {
            return types.SyntaxValues
                .GetValueOrNone(path)
                .SelectMany(x => x.AsType(types))
                .SelectMany(x => x.AsVariable(types))
                .TryGetValue(out type);
        }

        public static bool TryGetFunction(this ITypedFrame types, IdentifierPath path, out FunctionType type) {
            return types.SyntaxValues
                .GetValueOrNone(path)
                .SelectMany(x => x.AsType(types))
                .SelectMany(x => x.AsFunction(types))
                .TryGetValue(out type);
        }

        public static Option<PointerType> AsVariable(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) is PointerType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<FunctionType> AsFunction(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) is FunctionType funcSig) {
                return funcSig;
            }
            else {
                return Option.None;
            }            
        }

        public static Option<StructType> AsStruct(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) is StructType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<UnionType> AsUnion(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) is UnionType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static Option<ArrayType> AsArray(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) is ArrayType sig) {
                return sig;
            }
            else {
                return Option.None;
            }
        }

        public static bool IsBool(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) == PrimitiveType.Bool) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsInt(this HelixType type, ITypedFrame types) {
            if (type.GetSignatureSupertype(types) == PrimitiveType.Int) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsTypeChecked(this ISyntaxTree syntax, ITypedFrame types) {
            return types.ReturnTypes.ContainsKey(syntax);
        }

        public static HelixType GetReturnType(this ISyntaxTree syntax, ITypedFrame types) {
            return types.ReturnTypes[syntax];
        }

        public static void SetReturnType(this ISyntaxTree syntax, HelixType type, ITypedFrame types) {
            types.ReturnTypes[syntax] = type;
        }

        public static IReadOnlyList<VariableCapture> GetCapturedVariables(this ISyntaxTree syntax, ITypedFrame types) {
            return types.CapturedVariables[syntax];
        }

        public static void SetCapturedVariables(this ISyntaxTree syntax, ITypedFrame types) {
            types.CapturedVariables[syntax] = Array.Empty<VariableCapture>();
        }

        public static void SetCapturedVariables(this ISyntaxTree syntax, ISyntaxTree child, ITypedFrame types) {
            types.CapturedVariables[syntax] = child.GetCapturedVariables(types);
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            ISyntaxTree child1, 
            ISyntaxTree child2, 
            ITypedFrame types) {

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
            ITypedFrame types) {

            var caps = child1.GetCapturedVariables(types)
                .Concat(child2.GetCapturedVariables(types))
                .Concat(child3.GetCapturedVariables(types))
                .ToArray();

            types.CapturedVariables[syntax] = caps;
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IEnumerable<ISyntaxTree> children,
            ITypedFrame types) {

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
            ITypedFrame types) {

            types.CapturedVariables[syntax] = new[] { new VariableCapture(variable, kind, sig) };
        }

        public static void SetCapturedVariables(
            this ISyntaxTree syntax,
            IEnumerable<VariableCapture> caps,
            ITypedFrame types) {

            types.CapturedVariables[syntax] = caps.ToArray();
        }

        public static Bundle<HelixType> GetMembers(this HelixType type, ITypedFrame types) {
            var dict = new Dictionary<IdentifierPath, HelixType>();

            foreach (var (memPath, memType) in GetMemberPaths(type, types)) {
                dict[memPath] = memType;
            }

            return new Bundle<HelixType>(dict);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(
            HelixType type,
            ITypedFrame types) {

            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath,
            HelixType type,
            ITypedFrame types) {

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
