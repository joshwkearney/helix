using Helix.Analysis.TypeChecking;
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

        public override PassingSemantics GetSemantics(TypeFrame types) {
            switch (this.Kind) {
                case NominalTypeKind.Function:
                    return PassingSemantics.ValueType;
                default:
                    return types.NominalSignatures[this.Path].GetSemantics(types);
            }
        }

        public override HelixType GetMutationSupertype(TypeFrame types) {
            if (this.Kind == NominalTypeKind.Variable) {
                return this.GetSignatureSupertype(types).GetMutationSupertype(types);
            }
            else {
                return this;
            }
        }

        public override HelixType GetSignatureSupertype(TypeFrame types) {
            return types.NominalSignatures[this.Path].GetSignatureSupertype(types);
        }

        public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame types) {
            yield return this;

            foreach (var access in types.NominalSignatures[this.Path].GetAccessibleTypes(types)) {
                yield return access;
            }
        }

        public override Option<ISyntax> ToSyntax(TokenLocation loc, TypeFrame types) {
            return types.NominalSignatures[this.Path].ToSyntax(loc, types);
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }
    }
}