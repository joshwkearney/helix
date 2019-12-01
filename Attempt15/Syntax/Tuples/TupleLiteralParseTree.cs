using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoshuaKearney.Attempt15.Compiling;

namespace JoshuaKearney.Attempt15.Syntax.Tuples {
    public class ParseTupleMember {
        public string Name { get; }

        public IParseTree Value { get; }

        public ParseTupleMember(string name, IParseTree value) {
            this.Name = name;
            this.Value = value;
        }
    }

    public class TupleLiteralParseTree : IParseTree {
        public IReadOnlyList<ParseTupleMember> Members { get; }

        public TupleLiteralParseTree(IEnumerable<ParseTupleMember> members) {
            this.Members = members.ToArray();
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var members = this.Members.Select(x => {
                return new TupleMember(x.Name, x.Value.Analyze(args));
            })
            .ToArray();

            return new TupleLiteralSyntaxTree(members);
        }
    }
}