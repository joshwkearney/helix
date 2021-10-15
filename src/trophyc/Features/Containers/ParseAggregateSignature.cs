using System.Collections.Generic;
using Trophy.Analysis;

namespace Trophy.Features.Containers {
    public class ParseAggregateSignature  {
        public string Name { get; }

        public IReadOnlyList<ParseAggregateMember> Members { get; }

        public ParseAggregateSignature(string name, IReadOnlyList<ParseAggregateMember> mems) {
            this.Name = name;
            this.Members = mems;
        }
    }

    public class ParseAggregateMember {
        public string MemberName { get; }

        public ISyntaxA MemberType { get; }

        public VariableKind Kind { get; }

        public ParseAggregateMember(string name, ISyntaxA type, VariableKind kind) {
            this.MemberName = name;
            this.MemberType = type;
            this.Kind = kind;
        }
    }
}