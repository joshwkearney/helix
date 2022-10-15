using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Features.Aggregates {
    public class AggregateParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseAggregateMember> Members { get; }

        public AggregateParseSignature(string name, IReadOnlyList<ParseAggregateMember> mems) {
            this.Name = name;
            this.Members = mems;
        }

        public AggregateSignature ResolveNames(IdentifierPath scope, NamesRecorder names) {
            var mems = this.Members
                .Select(x => new AggregateMember(x.MemberName, x.MemberType.ResolveNames(scope, names)))
                .ToImmutableList();

            return new AggregateSignature(scope.Append(this.Name), mems);
        }
    }

    public class ParseAggregateMember {
        public string MemberName { get; }

        public ITypeTree MemberType { get; }

        public ParseAggregateMember(string name, ITypeTree type) {
            this.MemberName = name;
            this.MemberType = type;
        }
    }
}