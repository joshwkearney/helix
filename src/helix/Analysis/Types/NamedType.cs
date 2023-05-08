namespace Helix.Analysis.Types {
    public record NamedType : HelixType {
        public IdentifierPath Path { get; } 

        public NamedType(IdentifierPath fullName) {
            this.Path = fullName;
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

        public override bool IsValueType(ITypedFrame types) {
            if (types.Functions.ContainsKey(this.Path)) {
                return false;
            }

            if (types.Structs.TryGetValue(this.Path, out var sig)) {
                return sig.Members.All(x => x.Type.IsValueType(types));
            }

            throw new InvalidOperationException("Unexpected named type");
        }
    }
}