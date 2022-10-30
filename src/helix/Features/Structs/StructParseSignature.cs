using Helix.Analysis;
using Helix.Parsing;

namespace Helix.Features.Aggregates {
    public record StructParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseAggregateMember> Members { get; }

        public TokenLocation Location { get; }

        public StructParseSignature(TokenLocation loc, string name, IReadOnlyList<ParseAggregateMember> mems) {
            this.Name = name;
            this.Members = mems;
            this.Location = loc;
        }

        public StructSignature ResolveNames(EvalFrame types) {
            var path = types.ResolvePath(this.Location.Scope, this.Name);
            var mems = new List<AggregateMember>();

            foreach (var mem in this.Members) {
                if (!mem.MemberType.AsType(types).TryGetValue(out var type)) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(mem.Location);
                }

                mems.Add(new AggregateMember(mem.MemberName, type, mem.IsWritable));
            }

            return new StructSignature(path, mems);
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