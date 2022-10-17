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

        public static void DeclareSignatureNames(FunctionParseSignature sig, INamesRecorder names) {
            // Declare this function
            if (!names.DeclareName(sig.Name, NameTarget.Function)) {
                throw TypeCheckingErrors.IdentifierDefined(sig.Location, sig.Name);
            }

            names = names.WithScope(sig.Name);

            // Declare the parameters
            foreach (var par in sig.Parameters) {
                if (!names.DeclareName(par.Name, NameTarget.Variable)) {
                    throw TypeCheckingErrors.IdentifierDefined(par.Location, par.Name);
                }
            }
        }

        public static void DeclareSignaturePaths(FunctionSignature sig, ITypesRecorder paths) {
            // Declare this function
            paths.DeclareFunction(sig);
            paths.DeclareVariable(new VariableSignature(sig.Path, new FunctionType(sig), false));

            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var type = sig.Parameters[i].Type;
                var path = sig.Path.Append(parsePar.Name);

                paths.DeclareVariable(new VariableSignature(path, type, parsePar.IsWritable));
            }
        }
    }
}