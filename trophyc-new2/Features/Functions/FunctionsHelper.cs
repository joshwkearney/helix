using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Variables;
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

        public static void DeclareName(FunctionParseSignature sig, ITypesRecorder types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(sig.Name).HasValue) {
                throw TypeCheckingErrors.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare this function
            var path = types.CurrentScope.Append(sig.Name);

            types.SetValue(path, new TypeSyntax(sig.Location, new NamedType(path)));
            //names = names.WithScope(sig.Name);

            // Declare the parameters
            //foreach (var par in sig.Parameters) {
            //    if (names.TryResolvePath(par.Name).HasValue) {
            //        throw TypeCheckingErrors.IdentifierDefined(par.Location, par.Name);
            //    }

            //    names.SetValue(path, new DummySyntax(par.Location));
            //}
        }

        public static void DeclareParameters(FunctionSignature sig, ITypesRecorder types) {
            // Declare this function
            //paths.DeclareFunction(sig);
            //paths.DeclareVariable(new VariableSignature(sig.Path, new FunctionType(sig), false));

            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var type = sig.Parameters[i].Type;
                var path = types.CurrentScope.Append(parsePar.Name);

                if (parsePar.IsWritable) {
                    type = type.ToMutableType();
                }

                types.DeclareVariable(new VariableSignature(path, type, parsePar.IsWritable));
                types.SetValue(path, new VariableAccessSyntax(default, path));
            }
        }
    }
}