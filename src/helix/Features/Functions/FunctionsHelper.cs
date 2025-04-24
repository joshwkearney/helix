using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Types;
using Helix.Parsing;

namespace Helix.Features.Functions {
    public static class FunctionsHelper {
        public static void CheckForDuplicateParameters(TokenLocation loc, IEnumerable<string> pars) {
            var dups = pars
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (dups.Length > 0) {
                throw TypeException.IdentifierDefined(loc, dups.First());
            }
        }

        public static void DeclareName(ParseFunctionSignature sig, TypeFrame types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(types.Scope, sig.Name, out _)) {
                throw TypeException.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare this function
            var path = types.Scope.Append(sig.Name);
            var named = new NominalType(path, NominalTypeKind.Function);

            types.Locals = types.Locals.SetItem(path, new LocalInfo(named));
        }

        public static void DeclareParameters(FunctionType sig, IdentifierPath path, TypeFrame types) {
            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var parPath = path.Append(parsePar.Name);
                var parType = sig.Parameters[i].Type;
                var parSig = new PointerType(parType);

                // Declare this parameter as a root by making an end cycle in the graph
                foreach (var (relPath, memType) in parType.GetMembers(types)) {
                    var memPath = parPath.Append(relPath);

                    // Put these lifetimes in the main table
                    types.Locals = types.Locals.SetItem(
                        memPath, 
                        new LocalInfo(parSig));

                    types.NominalSignatures.Add(memPath, parSig);
                }
            }
        }
    }
}