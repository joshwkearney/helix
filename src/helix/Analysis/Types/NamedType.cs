using Helix.Analysis.TypeChecking;
using Helix.Features.Aggregates;
using Helix.Syntax;

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
                return this.GetAggregateSemantics(sig, types);
            }
            else if (types.Unions.TryGetValue(this.Path, out sig)) {
                return this.GetAggregateSemantics(sig, types);
            }

            throw new InvalidOperationException("Unexpected named type");
        }

        private PassingSemantics GetAggregateSemantics(StructSignature sig, ITypedFrame types) {
            var memSemantics = sig.Members.Select(x => x.Type.GetSemantics(types));

            if (memSemantics.All(x => x.IsValueType())) {
                return PassingSemantics.ValueType;
            }
            else {
                return PassingSemantics.ContainsReferenceType;
            }
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame types) {
            if (types.Structs.TryGetValue(this.Path, out var sig)) {
                return sig.Members
                    .SelectMany(x => x.Type.GetContainedTypes(types))
                    .Prepend(this);
            }
            else if (types.Unions.TryGetValue(this.Path, out sig)) {
                return sig.Members
                    .SelectMany(x => x.Type.GetContainedTypes(types))
                    .Prepend(this);
            }

            return new[] { this };
        }
    }
}