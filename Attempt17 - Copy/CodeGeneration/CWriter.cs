using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.CodeGeneration {
    public class CWriter : ICodeWriter {
        private ImmutableList<string> lines = ImmutableList<string>.Empty;

        public static string Dereference(string expression) {
            if (expression.StartsWith("&")) {
                return expression.Substring(1);
            }
            else {
                return "*" + expression;
            }
        }

        public static string AddressOf(string expression) {
            if (expression.StartsWith("*")) {
                return expression.Substring(1);
            }
            else {
                return "&" + expression;
            }
        }

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

        public ICodeWriter Line(string line) {
            this.lines = this.lines.Add(line);
            return this;
        }

        public ImmutableList<string> ToLines() {
            return this.lines;
        }

        public CBlock ToBlock(string returnValue) {
            return new CBlock(returnValue, this.lines);
        }
    }
}