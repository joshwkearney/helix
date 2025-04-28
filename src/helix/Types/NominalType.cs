using Helix.Parsing;
using Helix.Syntax;
using Helix.TypeChecking;

namespace Helix.Types {
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
            return this.GetValue(types).GetSemantics(types);
        }

        public override HelixType GetSignature(TypeFrame types) {
            return types.Signatures[this.Path];
        }

        public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame types) {
            yield return this;

            foreach (var access in this.GetValue(types).GetAccessibleTypes(types)) {
                yield return access;
            }
        }

        public override Option<ITypedTree> ToSyntax(TokenLocation loc, TypeFrame types) {
            return this.GetValue(types).ToSyntax(loc, types);
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override Option<PointerType> AsVariable(TypeFrame types) {
            return this.GetValue(types).AsVariable(types);
        }

        public override Option<FunctionType> AsFunction(TypeFrame types) {
            return this.GetValue(types).AsFunction(types);
        }

        public override Option<StructType> AsStruct(TypeFrame types) {
            return this.GetValue(types).AsStruct(types);
        }

        public override Option<UnionType> AsUnion(TypeFrame types) {
            return this.GetValue(types).AsUnion(types);
        }

        public override Option<ArrayType> AsArray(TypeFrame types) {
            return this.GetValue(types).AsArray(types);
        }

        public override bool IsBool(TypeFrame types) {
            return this.GetValue(types).IsBool(types);
        }

        public override bool IsWord(TypeFrame types) {
            return this.GetValue(types).IsWord(types);
        }

        private HelixType GetValue(TypeFrame types) {
            if (types.Refinements.TryGetValue(this.Path, out var value)) {
                return value;
            }
            else {
                return types.Signatures[this.Path];
            }
        }
    }
}