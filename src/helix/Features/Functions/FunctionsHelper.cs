using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Variables;
using Helix.Parsing;

namespace Helix.Features.Functions {
    public static class FunctionsHelper {
        public static void CheckForDuplicateParameters(TokenLocation loc, IEnumerable<string> pars) {
            var dups = pars
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (dups.Any()) {
                throw TypeException.IdentifierDefined(loc, dups.First());
            }
        }

        public static void DeclareName(FunctionParseSignature sig, TypeFrame types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(sig.Location.Scope, sig.Name, out _)) {
                throw TypeException.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare this function
            var path = sig.Location.Scope.Append(sig.Name);

            types.SyntaxValues[path] = new TypeSyntax(sig.Location, new NamedType(path));
        }

        public static void DeclareParameterTypes(TokenLocation loc, FunctionSignature sig, TypeFrame types) {
            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var type = sig.Parameters[i].Type;

                if (parsePar.IsWritable) {
                    type = type.ToMutableType();
                }

                // Declare this parameter as a root by making an end cycle in the graph
                foreach (var (relPath, memType) in type.GetMembers(types)) {
                    var path = sig.Path.Append(parsePar.Name).Append(relPath);
                    var lifetime = new Lifetime(path, 0);

                    types.Variables[path] = new VariableSignature(path, type, parsePar.IsWritable);
                    types.SyntaxValues[path] = new VariableAccessSyntax(loc, path);

                    if (!memType.IsValueType(types)) {
                        types.LifetimeRoots[path] = lifetime;
                    }
                }
            }
        }

        public static void DeclareParameterFlow(TokenLocation loc, FunctionSignature sig, FlowFrame flow) {
            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var type = sig.Parameters[i].Type;

                if (parsePar.IsWritable) {
                    type = type.ToMutableType();
                }

                // Declare this parameter as a root by making an end cycle in the graph
                foreach (var (relPath, memType) in type.GetMembers(flow)) {
                    var path = sig.Path.Append(parsePar.Name).Append(relPath);
                    var lifetime = new Lifetime(path, 0);

                    flow.LifetimeGraph.RequireOutlives(Lifetime.Stack, lifetime);
                    flow.VariableLifetimes[path] = lifetime;
                }
            }
        }
    }
}