using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoshuaKearney.Attempt15.Compiling {
    public class CMember {
        public string Name { get; }

        public string Type { get; }

        public CMember(string name, string type) {
            this.Name = name;
            this.Type = type;
        }
    }

    public enum CModifier {
        Inline = 1,
        Const = 2
    }

    public static class CSyntax {
        public static ICodeGenerator Statement(this ICodeGenerator gen, string stat) {
            gen.CodeBlocks.Peek().Add(stat);
            return gen;
        }

        public static string Malloc(this ICodeGenerator gen, string type) {
            return $"({type}*)malloc(sizeof({type}))";
        }

        public static ICodeGenerator Function(
            this ICodeGenerator gen, 
            string name, 
            IEnumerable<CMember> pars, 
            IEnumerable<string> body, 
            CModifier flags = 0
        ) {
            return gen.Function(name, "void", pars, body, null, flags);
        }

        public static ICodeGenerator Function(
            this ICodeGenerator gen,
            string name, 
            string returnType,
            IEnumerable<CMember> pars, 
            IEnumerable<string> body, 
            string returnValue = null, 
            CModifier flags = 0
        ) { 
            var result = new List<string>();

            StringBuilder sb = new StringBuilder();

            if (flags.HasFlag(CModifier.Inline)) {
                sb.Append("inline ");
            }

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

            gen.GlobalCode.AddRange(result);
            return gen;
        }

        public static ICodeGenerator TypedefFunctionPointer(this ICodeGenerator gen, string name, string returnType, IEnumerable<string> paramTypes) {
            StringBuilder sb = new StringBuilder();

            sb.Append("typedef ")
                .Append(returnType)
                .Append("(*")
                .Append(name)
                .Append(")(")
                .AppendJoin(", ", paramTypes)
                .Append(");")
                .AppendLine();

            gen.GlobalCode.Add(sb.ToString());
            return gen;
        }

        public static ICodeGenerator TypedefStruct(this ICodeGenerator gen, string name, IEnumerable<CMember> members) {
            var result = new List<string> {
                $"typedef struct {name} {{"
            };

            foreach (var member in members) {
                result.Add("    " + member.Type + " " + member.Name + ";");
            }

            result.Add($"}} {name};");
            result.Add("");

            gen.GlobalCode.AddRange(result);
            return gen;
        }

        public static string FunctionCall(this ICodeGenerator gen, string func, IEnumerable<string> args) {
            StringBuilder sb = new StringBuilder();

            sb.Append("")
                .Append(func)
                .Append("(")
                .AppendJoin(", ", args)
                .Append(")");

            return sb.ToString();
        }

        public static ICodeGenerator Declaration(this ICodeGenerator gen, string type, string name, string value, CModifier flags = 0) {
            string result = $"{(flags.HasFlag(CModifier.Const) ? "const " : "")}{type} {name} = {value};";
            gen.CodeBlocks.Peek().Add(result);

            return gen;
        }

        public static ICodeGenerator Declaration(this ICodeGenerator gen, string type, string name, CModifier flags = 0) {
            string result =  $"{(flags.HasFlag(CModifier.Const) ? "const " : "")}{type} {name};";
            gen.CodeBlocks.Peek().Add(result);

            return gen;
        }

        public static ICodeGenerator Assignment(this ICodeGenerator gen, string left, string right) {
            string result = left + " = " + right + ";";
            gen.CodeBlocks.Peek().Add(result);

            return gen;
        }

        public static ICodeGenerator IfStatement(this ICodeGenerator gen, string condition, IEnumerable<string> affirmBlock, IEnumerable<string> negBlock = null) {
            var result = new List<string>();

            result.Add($"if ({condition}) {{");
            result.AddRange(affirmBlock.Select(x => "    " + x));
            result.Add("}");

            if (negBlock != null && negBlock.Any()) {
                result.Add("else {");
                result.AddRange(negBlock.Select(x => "    " + x));
                result.Add("}");
            }

            gen.CodeBlocks.Peek().AddRange(result);
            return gen;
        }
    }
}