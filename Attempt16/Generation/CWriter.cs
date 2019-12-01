using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt16.Generation {
    public class CWriter {
        public static ImmutableList<string> Indent(IEnumerable<string> code) {
            return code.Select(x => "    " + x).ToImmutableList();
        }

        public static ImmutableList<string> Indent(params string[] code) {
            return code.Select(x => "    " + x).ToImmutableList();
        }

        public static ImmutableList<string> Trim(ImmutableList<string> code) {
            var rev = code.Reverse();
            int blank = rev.TakeWhile(string.IsNullOrWhiteSpace).Count();

            return code.Take(rev.Count - blank).ToImmutableList();
        }

        public static string Dereference(string expression) {
            if (expression.StartsWith("&")) {
                return expression.Substring(1);
            }
            else {
                return "*" + expression;
            }
        }

        public ImmutableList<string> SourceCode { get; private set; } = ImmutableList<string>.Empty;

        public ImmutableList<string> HeaderCode { get; private set; } = ImmutableList<string>.Empty;

        public CWriter() { }

        public CWriter(CCode code) {
            this.SourceCode = code.SourceLines;
            this.HeaderCode = code.HeaderLines;
        }

        public CCode ToCCode(string returnValue) {
            return new CCode(
                returnValue,
                this.SourceCode,
                this.HeaderCode
            );
        }

        public CWriter Append(CCode other) {
            this.SourceLines(other.SourceLines);
            this.HeaderLines(other.HeaderLines);

            return this;
        }

        public CWriter Append(CType other) {
            this.HeaderLines(other.HeaderLines);

            return this;
        }

        public CWriter Append(CWriter other) {
            this.SourceLines(other.SourceCode);
            this.HeaderLines(other.HeaderCode);

            return this;
        }


        public CWriter ForwardDeclaration(string name, string returnType, IEnumerable<(string type, string name)> pars) {
            string line = returnType + " " + name + "(";

            line += string.Join(", ", pars.Select(x => x.type + " " + x.name));
            line += ");";

            this.HeaderLine(line);
            this.HeaderLine();

            return this;
        }

        public CWriter VariableDeclaration(string type, string name, string value) {
            return this.SourceLine($"{type} {name} = {value};");
        }

        public CWriter SourceEmptyLine() {
            return this.SourceLine(string.Empty);
        }

        public CWriter SourceLine(string line) {
            this.SourceCode = this.SourceCode.Add(line);
            return this;
        }

        public CWriter HeaderLine(string line) {
            this.HeaderCode = this.HeaderCode.Add(line);
            return this;
        }

        public CWriter HeaderLine() {
            this.HeaderCode = this.HeaderCode.Add("");
            return this;
        }


        public CWriter Assignment(string lhs, string rhs) {
            return this.SourceLine($"{lhs} = {rhs};");
        }

        public CWriter SourceLines(IEnumerable<string> lines) {
            this.SourceCode = this.SourceCode.AddRange(lines);
            return this;
        }

        public CWriter HeaderLines(IEnumerable<string> lines) {
            this.HeaderCode = this.HeaderCode.AddRange(lines);
            return this;
        }

        public CWriter ReturnStatement(string value) {
            return this.SourceLine($"return {value};");
        }
    }
}
