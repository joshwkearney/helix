using Helix.Analysis.TypeChecking;
using Helix.Features.Types;
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

        public override HelixType GetSignature(TypeFrame types) {
            return types.NominalSignatures[this.Path].GetSignature(types);
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

        public override Option<PointerType> AsVariable(TypeFrame types) {
            return types.NominalSignatures[this.Path].AsVariable(types);
        }

        public override Option<FunctionType> AsFunction(TypeFrame types) {
            return types.NominalSignatures[this.Path].AsFunction(types);
        }

        public override Option<StructType> AsStruct(TypeFrame types) {
            return types.NominalSignatures[this.Path].AsStruct(types);
        }

        public override Option<UnionType> AsUnion(TypeFrame types) {
            return types.NominalSignatures[this.Path].AsUnion(types);
        }

        public override Option<ArrayType> AsArray(TypeFrame types) {
            return types.NominalSignatures[this.Path].AsArray(types);
        }

        public override bool IsBool(TypeFrame types) {
            return types.NominalSignatures[this.Path].IsBool(types);
        }

        public override bool IsWord(TypeFrame types) {
            return types.NominalSignatures[this.Path].IsWord(types);
        }
    }
}