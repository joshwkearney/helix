using Attempt19.Types;
using System.Collections.Immutable;

namespace Attempt19.CodeGeneration {
    public static class ScopeHelper {
        public static ImmutableList<string> CleanupScope(ImmutableDictionary<string, LanguageType> varsToCleanUp, ICodeGenerator gen) {
            var lines = ImmutableList<string>.Empty;

            foreach (var (name, type) in varsToCleanUp) {
                if (type.GetCopiability() == TypeCopiability.Unconditional) {
                    continue;
                }

                if (!gen.GetDestructor(type).TryGetValue(out var destructor)) {
                    continue;
                }

                lines = lines.Add($"{destructor}({name});");
            }

            return lines;
        }
    }
}