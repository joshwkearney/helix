using Helix.Analysis.TypeChecking;
using Helix.Features.Aggregates;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public enum NominalTypeKind {
        Function, Struct, Union, Variable
    }

    public record NominalType(string Name, NominalTypeKind Kind) : HelixType {
        public override PassingSemantics GetSemantics(TypeFrame types) {
            switch (this.Kind) {
                case NominalTypeKind.Function:
                    return PassingSemantics.ValueType;
                default:
                    return types.Locals[this.Name].Type.GetSemantics(types);
            }
        }

        public override HelixType GetMutationSupertype(TypeFrame types) {
            if (this.Kind == NominalTypeKind.Variable) {
                return this.GetSignature(types).GetMutationSupertype(types);
            }
            else {
                return this;
            }
        }

        public override HelixType GetSignature(TypeFrame types) {
            return types.Locals[this.Name].Type.GetSignature(types);
        }

        public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame types) {
            yield return this;

            foreach (var access in types.Locals[this.Name].Type.GetAccessibleTypes(types)) {
                yield return access;
            }
        }

        public override Option<IParseTree> ToSyntax(TokenLocation loc, TypeFrame types) {
            return types.Locals[this.Name].Type.ToSyntax(loc, types);
        }

        public override string ToString() {
            return this.Name;
        }
    }
}