using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public class CMember {
        public string Name { get; }

        public string Type { get; }

        public CMember(string name, string type) {
            this.Name = name;
            this.Type = type;
        }
    }

    public static class CSyntax {
        public static IReadOnlyList<string> Function(string name, string returnType, IEnumerable<CMember> pars, IEnumerable<string> body, string returnValue) {
            var result = new List<string>();

            StringBuilder sb = new StringBuilder();
            sb.Append(returnType)
                .Append(" ")
                .Append(name)
                .Append("(")
                .AppendJoin(", ", pars.Select(x => x.Type + " " + x.Name))
                .Append(") {");

            result.Add(sb.ToString());

            result.AddRange(body.Select(x => "    " + x));

            if (!string.IsNullOrWhiteSpace(returnValue)) {
                result.Add("    return " + returnValue + ";");
            }

            result.Add("}");
            result.Add("");

            return result;
        }

        public static IReadOnlyList<string> TypedefFunctionPointer(string name, string returnType, IEnumerable<string> paramTypes) {
            StringBuilder sb = new StringBuilder();

            sb.Append("typedef ")
                .Append(returnType)
                .Append("(*")
                .Append(name)
                .Append(")(")
                .AppendJoin(", ", paramTypes)
                .Append(");");

            return new[] { sb.ToString(), "" };
        }

        public static IReadOnlyList<string> TypedefStruct(string name, IEnumerable<CMember> members) {
            var result = new List<string> {
                $"typedef struct {name} {{"
            };

            foreach (var member in members) {
                result.Add("    " + member.Type + " " + member.Name + ";");
            }

            result.Add($"}} {name};");
            result.Add("");

            return result;
        }

        public static string FunctionCall(string func, IEnumerable<string> args) {
            StringBuilder sb = new StringBuilder();

            sb.Append("")
                .Append(func)
                .Append("(")
                .AppendJoin(", ", args)
                .Append(")");

            return sb.ToString();
        }

        public static string Declaration(string type, string name, string value, bool constant = true) {
            return $"{(constant ? "const " : "")}{type} {name} = {value};";
        }

        public static string Declaration(string type, string name, bool constant = true) {
            return $"{(constant ? "const " : "")}{type} {name};";
        }

        public static string Assignment(string left, string right) {
            return left + " = " + right + ";";
        }

        public static IReadOnlyList<string> IfStatement(string condition, IEnumerable<string> affirmBlock, IEnumerable<string> negBlock) {
            var result = new List<string>();

            result.Add($"if ({condition}) {{");
            result.AddRange(affirmBlock.Select(x => "    " + x));
            result.Add("}");
            result.Add("else {");
            result.AddRange(negBlock.Select(x => "    " + x));
            result.Add("}");

            return result;
        }
    }
}