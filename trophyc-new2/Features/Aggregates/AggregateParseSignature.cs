using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Features.Aggregates {
    public record AggregateParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseAggregateMember> Members { get; }

        public AggregateParseSignature(string name, IReadOnlyList<ParseAggregateMember> mems) {
            this.Name = name;
            this.Members = mems;
        }

        public AggregateSignature ResolveNames(INamesRecorder names) {
            var path = names.TryFindPath(this.Name).GetValue();
            var mems = new List<AggregateMember>();

            foreach (var mem in this.Members) {
                if (!mem.MemberType.ToType(names).TryGetValue(out var type)) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(mem.Location);
                }

                mems.Add(new AggregateMember(mem.MemberName, type, mem.IsWritable));
            }

            return new AggregateSignature(path, mems);
        }
    }

    public record ParseAggregateMember {
        public string MemberName { get; }

        public ISyntax MemberType { get; }

        public TokenLocation Location { get; }

        public bool IsWritable { get; }

        public ParseAggregateMember(TokenLocation loc, string name, ISyntax type, bool isWritable) {
            this.Location = loc;
            this.MemberName = name;
            this.MemberType = type;
            this.IsWritable = isWritable;
        }
    }
}