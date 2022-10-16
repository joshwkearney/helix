using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Features.Aggregates {
    public class AggregateParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseAggregateMember> Members { get; }

        public AggregateParseSignature(string name, IReadOnlyList<ParseAggregateMember> mems) {
            this.Name = name;
            this.Members = mems;
        }

        public AggregateSignature ResolveNames(IdentifierPath scope, TypesRecorder types) {
            var mems = new List<AggregateMember>();

            foreach (var mem in this.Members) {
                if (!mem.MemberType.ToType(scope, types).TryGetValue(out var type)) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(mem.Location);
                }
            }

            return new AggregateSignature(scope.Append(this.Name), mems);
        }
    }

    public class ParseAggregateMember {
        public string MemberName { get; }

        public ISyntaxTree MemberType { get; }

        public TokenLocation Location { get; }

        public ParseAggregateMember(TokenLocation loc, string name, ISyntaxTree type) {
            this.Location = loc;
            this.MemberName = name;
            this.MemberType = type;
        }
    }
}