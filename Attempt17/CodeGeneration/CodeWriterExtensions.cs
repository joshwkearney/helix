using System.Collections.Generic;

namespace Attempt17.CodeGeneration {
    public static class CodeWriterExtensions {
        public static ICodeWriter EmptyLine(this ICodeWriter writer) {
            return writer.Line(string.Empty);
        }

        public static ICodeWriter Lines(this ICodeWriter writer, IEnumerable<string> lines) {
            foreach (var line in lines) {
                writer.Line(line);
            }

            return writer;
        }

        public static ICodeWriter VariableInit(this ICodeWriter writer, string type, string name, string value = null) {
            if (value == null) {
                return writer.Line($"{type} {name};");
            }
            else {
                return writer.Line($"{type} {name} = {value};");
            }
        }

        public static ICodeWriter VariableAssignment(this ICodeWriter writer, string target, string value) {
            return writer.Line($"{target} = {value};");
        }
    }
}