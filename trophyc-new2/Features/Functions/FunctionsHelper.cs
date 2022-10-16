using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public static class FunctionsHelper {
        public static void CheckForDuplicateParameters(TokenLocation loc, IEnumerable<string> pars) {
            var dups = pars
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(loc, dups.First());
            }
        }

        public static void DeclareSignatureNames(FunctionParseSignature sig, IdentifierPath scope, TypesRecorder types) {
            // Declare this function
            if (!types.TrySetNameTarget(scope, sig.Name, NameTarget.Function)) {
                throw TypeCheckingErrors.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare the parameters
            foreach (var par in sig.Parameters) {
                var path = scope.Append(sig.Name);

                if (!types.TrySetNameTarget(path, par.Name, NameTarget.Variable)) {
                    throw TypeCheckingErrors.IdentifierDefined(par.Location, par.Name);
                }
            }
        }

        public static void DeclareSignatureTypes(FunctionSignature sig, IdentifierPath scope, TypesRecorder types) {
            // Declare this function
            types.SetFunction(sig);
            types.SetVariable(sig.Path, new FunctionType(sig), false);

            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var type = sig.Parameters[i].Type;
                var path = sig.Path.Append(parsePar.Name);

                types.SetVariable(path, type, parsePar.IsWritable);
            }
        }
    }
}