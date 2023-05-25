using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Parsing;

namespace Helix.Features.Aggregates
{
    public record StructParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseStructMember> Members { get; }

        public TokenLocation Location { get; }

        public StructParseSignature(TokenLocation loc, string name, IReadOnlyList<ParseStructMember> mems) {
            this.Name = name;
            this.Members = mems;
            this.Location = loc;
        }

        public StructSignature ResolveNames(TypeFrame types) {
            var path = types.ResolvePath(this.Location.Scope, this.Name);
            var mems = new List<StructMember>();

            foreach (var mem in this.Members) {
                if (!mem.MemberType.AsType(types).TryGetValue(out var type)) {
                    throw TypeException.ExpectedTypeExpression(mem.Location);
                }

                mems.Add(new StructMember(mem.MemberName, type, mem.IsWritable));
            }

            return new StructSignature(path, mems);
        }
    }

    public record ParseStructMember {
        public string MemberName { get; }

        public ISyntaxTree MemberType { get; }

        public TokenLocation Location { get; }

        public bool IsWritable { get; }

        public ParseStructMember(TokenLocation loc, string name, ISyntaxTree type, bool isWritable) {
            this.Location = loc;
            this.MemberName = name;
            this.MemberType = type;
            this.IsWritable = isWritable;
        }
    }
}