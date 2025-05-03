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
        if (types.StructSignatures.TryGetValue(this.Path, out var structSig)) {
            return structSig.GetSemantics(types);
        }
        else if (types.UnionSignatures.TryGetValue(this.Path, out var unionSig)) {
            return unionSig.GetSemantics(types);
        }
        else if (types.FunctionSignatures.ContainsKey(this.Path)) {
            return PassingSemantics.ReferenceType;
        }
        else if (types.VariableRefinements.TryGetValue(this.Path, out var refinement)) {
            return refinement.GetSemantics(types);
        }
        else {
            throw new InvalidOperationException();
        }
    }

    public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame types) {
        yield return this;

        if (types.StructSignatures.TryGetValue(this.Path, out var structSig)) {
            foreach (var access in structSig.GetAccessibleTypes(types)) {
                yield return access;
            }
        }
        else if (types.UnionSignatures.TryGetValue(this.Path, out var unionSig)) {
            foreach (var access in unionSig.GetAccessibleTypes(types)) {
                yield return access;
            }
        }
        else if (types.VariableRefinements.TryGetValue(this.Path, out var refinement)) {
            foreach (var access in refinement.GetAccessibleTypes(types)) {
                yield return access;
            }
        }
    }

    public override Option<ITypedExpression> ToSyntax(TokenLocation loc, TypeFrame types) {
        return types.VariableRefinements.GetValueOrNone(this.Path).SelectMany(x => x.ToSyntax(loc, types));
    }

    public override string ToString() {
        return this.Path.Segments.Last();
    }

    public override Option<FunctionSignature> AsFunction(TypeFrame types) {
        return types.FunctionSignatures.GetValueOrNone(this.Path);
    }

    public override Option<StructSignature> AsStruct(TypeFrame types) {
        return types.StructSignatures.GetValueOrNone(this.Path);
    }

    public override Option<UnionSignature> AsUnion(TypeFrame types) {
        return types.UnionSignatures.GetValueOrNone(this.Path);
    }

    public override Option<ArrayType> AsArray(TypeFrame types) {
        return types.VariableRefinements.GetValueOrNone(this.Path).SelectMany(x => x.AsArray(types));
    }

    public override Option<ReferenceType> AsReference(TypeFrame types) {
        return types.VariableRefinements.GetValueOrNone(this.Path).SelectMany(x => x.AsReference(types));
    }

    public override bool IsBool(TypeFrame types) {
        return types.VariableRefinements
            .GetValueOrNone(this.Path).Select(x => x.IsBool(types))
            .OrElse(() => false);
    }

    public override bool IsWord(TypeFrame types) {
        return types.VariableRefinements
            .GetValueOrNone(this.Path).Select(x => x.IsWord(types))
            .OrElse(() => false);
    }

    public override HelixType GetSupertype(TypeFrame types) {
        if (types.VariableRefinements.TryGetValue(this.Path, out var refinement)) {
            return refinement;
        }

        return this;
    }
}