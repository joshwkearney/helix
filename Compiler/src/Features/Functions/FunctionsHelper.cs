using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public static class FunctionsHelper {
        public static void CheckForDuplicateParameters(
            TokenLocation loc, 
            IEnumerable<string> pars) {

            var dups = pars
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(loc, dups.First());
            }
        }

        public static ISyntaxB ResolveBodyNames(
            INamesRecorder names, 
            IdentifierPath funcPath, 
            IdentifierPath heapRegion,
            ISyntaxA body, 
            IEnumerable<FunctionParameter> pars) {

            var region = heapRegion.Append("stack");

            // Push this function name as the new scope
            var context = names.Context.WithScope(_ => funcPath).WithRegion(_ => region);
            var result = names.WithContext(context, names => {
                // Declare the parameters
                foreach (var par in pars) {
                    names.DeclareName(funcPath.Append(par.Name), NameTarget.Variable, IdentifierScope.LocalName);
                }

                // Pop the new scope out
                return body.CheckNames(names);
            });

            return result;
        }

        public static void DeclareParameters(
            ITypesRecorder types, 
            IdentifierPath funcPath, 
            IReadOnlyList<FunctionParameter> pars, 
            IReadOnlyList<int> parIds) {

            for (int i = 0; i < pars.Count; i++) {
                var par = pars[i];
                var id = parIds[i];
                var type = par.Type;
                var defKind = VariableDefinitionKind.Parameter;

                if (type is VarRefType varRefType) {
                    type = varRefType.InnerType;
                    defKind = varRefType.IsReadOnly ? VariableDefinitionKind.ParameterRef : VariableDefinitionKind.ParameterVar;
                }

                var path = funcPath.Append(par.Name);
                var info = new VariableInfo(
                    name: par.Name,
                    innerType: type,
                    kind: defKind,
                    id: id,
                    valueLifetimes: new[] { new IdentifierPath("$args_" + par.Name) }.ToImmutableHashSet(),
                    variableLifetimes: new[] { new IdentifierPath("$args_" + par.Name) }.ToImmutableHashSet());

                types.DeclareName(path, NamePayload.FromVariable(info));
            }
        }

        public static void CheckForInvalidReturnScope(TokenLocation loc, IdentifierPath heapRegion, ISyntaxC body) {
            foreach (var capLifetime in body.Lifetimes) {
                if (capLifetime.Segments.Any() && capLifetime.Segments.First().StartsWith("$args_")) {
                    continue;
                }

                if (!capLifetime.Outlives(heapRegion.Append("stack"))) {
                    throw TypeCheckingErrors.LifetimeExceeded(loc, heapRegion, capLifetime);
                }
            }
        }
    }
}