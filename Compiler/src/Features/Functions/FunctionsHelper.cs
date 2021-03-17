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
            INameRecorder names, 
            IdentifierPath funcPath, 
            ISyntaxA body, 
            IEnumerable<FunctionParameter> pars) {

            // Push this function name as the new scope
            names.PushScope(funcPath);
            names.PushRegion(IdentifierPath.StackPath);

            // Declare the parameters
            foreach (var par in pars) {
                names.DeclareLocalName(funcPath.Append(par.Name), NameTarget.Variable);
            }

            // Pop the new scope out
            var result = body.CheckNames(names);

            names.PopRegion();
            names.PopScope();

            return result;
        }

        public static void DeclareParameters(
            ITypeRecorder types, 
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

                types.DeclareVariable(path, info);
            }
        }

        public static void CheckForInvalidReturnScope(TokenLocation loc, ISyntaxC body) {
            foreach (var capLifetime in body.Lifetimes) {
                if (capLifetime.Segments.Any() && capLifetime.Segments.First().StartsWith("$args_")) {
                    continue;
                }

                if (!capLifetime.Outlives(IdentifierPath.StackPath)) {
                    throw TypeCheckingErrors.LifetimeExceeded(loc, IdentifierPath.HeapPath, capLifetime);
                }
            }
        }
    }
}