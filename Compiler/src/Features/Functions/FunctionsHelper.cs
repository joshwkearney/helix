using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trophy.Analysis;

namespace Trophy.Features.Functions {
    public static class FunctionsHelper {
        public static IReadOnlyList<string> FindDuplicateParameters(IEnumerable<FunctionParameter> pars) {
            var dups = pars
                .Select(x => x.Name)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            return dups;
        }
    }
}
