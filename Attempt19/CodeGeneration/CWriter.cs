using Attempt18.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Attempt18.CodeGeneration {
    public class CWriter : ICodeWriter {
        private ImmutableList<string> lines = ImmutableList<string>.Empty;

        public static string MaskPointer(string value) {
            if (value.EndsWith(" & ~1")) {
                return value;
            }
            else {
                return value + " & ~1";
            }
        }

        public static string Dereference(string expression, string valueType) {
            var reg = new Regex(@"\(uintptr_t\)\(&(.+)\)");
            var match = reg.Match(expression);

            if (match.Success) {
                return match.Groups[1].Value;
            }
            else {
                return $"*({valueType}*)({MaskPointer(expression)})";
            }
        }

        public static string AddressOf(string expression) {
            var reg = new Regex(@"\*\(.+\)\((.+) & ~1\)");
            var match = reg.Match(expression);

            if (match.Success) {
                return match.Groups[1].Value;
            }
            else {
                return "(uintptr_t)(&" + expression + ")";
            }
        }

        public static ImmutableList<string> Indent(IEnumerable<string> code) {
            return code.Select(x => "    " + x).ToImmutableList();
        }

        public static ImmutableList<string> Indent(params string[] code) {
            return Indent((IEnumerable<string>)code);
        }

        public static ImmutableList<string> Indent(int count, IEnumerable<string> code) {
            var spaces = new string(Enumerable.Repeat(' ', count * 4).ToArray());

            return code.Select(x => spaces + x).ToImmutableList();
        }

        public static ImmutableList<string> Indent(int count, params string[] code) {
            return Indent(count, (IEnumerable<string>)code);
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