using Helix.Analysis.TypeChecking;
using Helix.Features.Aggregates;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public enum NominalTypeKind {
        Function, Struct, Union, Variable
    }

    public record NominalType : HelixType {
        public IdentifierPath Path { get; } 

        public NominalTypeKind Kind { get; }

        public NominalType(IdentifierPath fullName, NominalTypeKind kind) {
            this.Path = fullName;
            this.Kind = kind;
        }

        public override HelixType GetNaturalSupertype(ITypedFrame types) {
            // TODO: Fix this
            if (types.NominalSupertypes.TryGetValue(this, out var s)) {
                return s.GetNaturalSupertype(types);
            }
            else {
                return this;
            }
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            switch (this.Kind) {
                case NominalTypeKind.Function:
                    return PassingSemantics.ValueType;
                case NominalTypeKind.Struct:
                    return this.GetAggregateSemantics(types.Structs[this.Path], types);
                case NominalTypeKind.Union:
                    return this.GetAggregateSemantics(types.Unions[this.Path], types);
                default:
                    throw new InvalidOperationException();
            }
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