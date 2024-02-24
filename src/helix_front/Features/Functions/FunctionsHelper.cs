using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Features.Types;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Syntax;
using System.IO;
using System.Runtime.CompilerServices;

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
            if (types.TryGetVariable(sig.Name, out _)) {
                throw TypeException.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare this function
            var named = new NominalType(sig.Name, NominalTypeKind.Function);

            types.Locals = types.Locals.SetItem(sig.Name, new LocalInfo(named));
        }

        public static void DeclareParameters(FunctionType sig, TypeFrame flow) {
            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var parType = sig.Parameters[i].Type;
                var parSig = new PointerType(parType);

                // Declare this parameter as a root by making an end cycle in the graph
                foreach (var (relPath, memType) in parType.GetMembers(flow)) {
                    flow.Locals = flow.Locals.SetItem(parsePar.Name, new LocalInfo(parSig));
                }
            }
        }
    }
}