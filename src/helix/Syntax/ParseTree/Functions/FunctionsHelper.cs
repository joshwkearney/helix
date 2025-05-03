using Helix.Parsing;
using Helix.Syntax.ParseTree;
using Helix.Types;

namespace Helix.TypeChecking;

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

        return types.WithDeclaration(path, named);
    }

    public static TypeFrame DeclareParameters(FunctionSignature sig, IdentifierPath path, TypeFrame types) {
        for (int i = 0; i < sig.Parameters.Count; i++) {
            var parsePar = sig.Parameters[i];
            var parPath = path.Append(parsePar.Name);
            var parType = sig.Parameters[i].Type;
            var parSig = new ReferenceType(parType);

            // TODO: Have another type for a variable?
            types = types.WithDeclaration(parPath, new NominalType(parPath, NominalTypeKind.Variable));
            types = types.WithVariableRefinement(parPath, parType);
        }

        return types;
    }
}