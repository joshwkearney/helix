using Helix.Analysis.TypeChecking;
using Helix.Features.Aggregates;
using Helix.Parsing;
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

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            switch (this.Kind) {
                case NominalTypeKind.Function:
                    return PassingSemantics.ValueType;
                default:
                    return types.NominalSignatures[this.Path].GetSemantics(types);
            }
        }

        public override HelixType GetMutationSupertype(ITypedFrame types) => this;

        public override HelixType GetSignatureSupertype(ITypedFrame types) {
            return types.NominalSignatures[this.Path].GetSignatureSupertype(types);
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame types) {
            return types.NominalSignatures[this.Path].GetContainedTypes(types);
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }
    }
}