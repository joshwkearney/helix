﻿using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.CodeGeneration {
    public static class ScopeHelper {
        public static ImmutableList<string> CleanupScope(ImmutableDictionary<string, LanguageType> varsToCleanUp, ICodeGenerator gen) {
            var lines = ImmutableList<string>.Empty;

            foreach (var (name, type) in varsToCleanUp) {
                if (!gen.GetDestructor(type).TryGetValue(out var destructor)) {
                    continue;
                }

                if (type is VariableType varType || type is ArrayType) {
                    lines = lines.Add($"{destructor}({name});");
                }
            }

            return lines;
        }
    }
}