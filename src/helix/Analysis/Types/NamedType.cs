using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record NamedType : HelixType {
        public IdentifierPath Path { get; } 

        public NamedType(IdentifierPath fullName) {
            this.Path = fullName;
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            if (types.Functions.ContainsKey(this.Path)) {
                return PassingSemantics.ValueType;
            }

            if (types.Structs.TryGetValue(this.Path, out var sig)) {
                var memSemantics = sig.Members.Select(x => x.Type.GetSemantics(types));

                if (memSemantics.All(x => x.IsValueType())) {
                    return PassingSemantics.ValueType;
                }
                else {
                    return PassingSemantics.ContainsReferenceType;
                }
            }

            throw new InvalidOperationException("Unexpected named type");
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override IEnumerable<HelixType> GetContainedTypes(EvalFrame types) {
            if (types.Structs.TryGetValue(this.Path, out var sig)) {
                return sig.Members
                    .SelectMany(x => x.Type.GetContainedTypes(types))
                    .Prepend(this);
            }

            return new[] { this };
        }

        public override UnificationKind TestUnification(HelixType other, EvalFrame types) {
            if (this == other) {
                return UnificationKind.Pun;
            }
            else {
                return UnificationKind.None;
            }
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, UnificationKind unificationKind, EvalFrame types) {
            if (this.TestUnification(other, types).IsSubsetOf(unificationKind)) {
                return syntax;
            }

            throw new InvalidOperationException();
        }
    }
}