using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;

namespace Helix.Types;

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
        return this.GetRefinement(types).GetSemantics(types);
    }

    public override HelixType GetSignature(TypeFrame types) {
        return types.Signatures[this.Path];
    }

    public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame types) {
        yield return this;

        foreach (var access in this.GetRefinement(types).GetAccessibleTypes(types)) {
            yield return access;
        }
    }

    public override Option<ITypedExpression> ToSyntax(TokenLocation loc, TypeFrame types) {
        return this.GetRefinement(types).ToSyntax(loc, types);
    }

    public override string ToString() {
        return this.Path.Segments.Last();
    }

    public override Option<FunctionType> AsFunction(TypeFrame types) {
        return this.GetRefinement(types).AsFunction(types);
    }

    public override Option<StructType> AsStruct(TypeFrame types) {
        return this.GetRefinement(types).AsStruct(types);
    }

    public override Option<UnionType> AsUnion(TypeFrame types) {
        return this.GetRefinement(types).AsUnion(types);
    }

    public override Option<ArrayType> AsArray(TypeFrame types) {
        return this.GetRefinement(types).AsArray(types);
    }

    public override Option<ReferenceType> AsReference(TypeFrame types) {
        return this.GetRefinement(types).AsReference(types);
    }


    public override bool IsBool(TypeFrame types) {
        return this.GetRefinement(types).IsBool(types);
    }

    public override bool IsWord(TypeFrame types) {
        return this.GetRefinement(types).IsWord(types);
    }

    private HelixType GetRefinement(TypeFrame types) {
        if (types.Refinements.TryGetValue(this.Path, out var value)) {
            return value;
        }
        else {
            return types.Signatures[this.Path];
        }
    }
}