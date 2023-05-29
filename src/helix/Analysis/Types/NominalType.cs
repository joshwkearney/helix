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

        public override PassingSemantics GetSemantics(ITypeContext types) {
            switch (this.Kind) {
                case NominalTypeKind.Function:
                    return PassingSemantics.ValueType;
                default:
                    return types.GlobalNominalSignatures[this.Path].GetSemantics(types);
            }
        }

        public override HelixType GetMutationSupertype(ITypeContext types) {
            if (this.Kind == NominalTypeKind.Variable) {
                return this.GetSignatureSupertype(types);
            }
            else {
                return this;
            }
        }

        public override HelixType GetSignatureSupertype(ITypeContext types) {
            return types.GlobalNominalSignatures[this.Path].GetSignatureSupertype(types);
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame types) {
            return types.NominalSignatures[this.Path].GetContainedTypes(types);
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }
    }
}