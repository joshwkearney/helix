using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Features.Aggregates {
    public record AggregateParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseAggregateMember> Members { get; }

        public AggregateKind Kind { get; }

        public TokenLocation Location { get; }

        public AggregateParseSignature(TokenLocation loc, string name, AggregateKind kind, IReadOnlyList<ParseAggregateMember> mems) {
            this.Name = name;
            this.Members = mems;
            this.Kind = kind;
            this.Location = loc;
        }

        public AggregateSignature ResolveNames(SyntaxFrame types) {
            var path = types.ResolvePath(this.Location.Scope, this.Name);
            var mems = new List<AggregateMember>();

            foreach (var mem in this.Members) {
                if (!mem.MemberType.AsType(types).TryGetValue(out var type)) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(mem.Location);
                }

                mems.Add(new AggregateMember(mem.MemberName, type, mem.IsWritable));
            }

            return new AggregateSignature(path, this.Kind, mems);
        }
    }

    public record ParseAggregateMember {
        public string MemberName { get; }

        public ISyntaxTree MemberType { get; }

        public TokenLocation Location { get; }

        public bool IsWritable { get; }

        public ParseAggregateMember(TokenLocation loc, string name, ISyntaxTree type, bool isWritable) {
            this.Location = loc;
            this.MemberName = name;
            this.MemberType = type;
            this.IsWritable = isWritable;
        }
    }
}