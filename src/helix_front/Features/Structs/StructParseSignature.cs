using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Parsing;
using Helix.Analysis.Types;
using Helix.Analysis;

namespace Helix.Features.Aggregates {
    public record StructParseSignature {
        public string Name { get; }

        public IReadOnlyList<ParseStructMember> Members { get; }

        public TokenLocation Location { get; }

        public StructParseSignature(TokenLocation loc, string name, IReadOnlyList<ParseStructMember> mems) {
            this.Name = name;
            this.Members = mems;
            this.Location = loc;
        }

        public StructType ResolveNames(TypeFrame types) {
            var mems = new List<StructMember>();

            foreach (var mem in this.Members) {
                if (!mem.MemberType.AsType(types).TryGetValue(out var type)) {
                    throw TypeException.ExpectedTypeExpression(mem.Location);
                }

                mems.Add(new StructMember(mem.MemberName, type));
            }

            return new StructType(mems);
        }
    }

    public record ParseStructMember {
        public string MemberName { get; }

        public IParseTree MemberType { get; }

        public TokenLocation Location { get; }

        public ParseStructMember(TokenLocation loc, string name, IParseTree type) {
            this.Location = loc;
            this.MemberName = name;
            this.MemberType = type;
        }
    }
}