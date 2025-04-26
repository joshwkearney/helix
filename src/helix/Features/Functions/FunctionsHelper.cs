using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
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

        public static TypeFrame DeclareName(ParseFunctionSignature sig, TypeFrame types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(types.Scope, sig.Name, out _)) {
                throw TypeException.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare this function
            var path = types.Scope.Append(sig.Name);
            var named = new NominalType(path, NominalTypeKind.Function);

            return types.WithDeclaration(path, DeclarationKind.Function, named);
        }

        public static TypeFrame DeclareParameters(FunctionType sig, IdentifierPath path, TypeFrame types) {
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var parPath = path.Append(parsePar.Name);
                var parType = sig.Parameters[i].Type;
                var parSig = new PointerType(parType);

                foreach (var (relPath, memType) in parType.GetMembers(types)) {
                    var memPath = parPath.Append(relPath);

                    types = types.WithDeclaration(memPath, DeclarationKind.Parameter, parSig);
                }
            }

            return types;
        }
    }
}