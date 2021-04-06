using System.Collections.Generic;

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

        public ParseAggregateMember(string name, ISyntaxA type) {
            this.MemberName = name;
            this.MemberType = type;
        }
    }
}