using System.Diagnostics;
using Helix.Collections;
using Helix.Types;

namespace Helix.TypeChecking.Predicates;

public record IsUnionMemberPredicate : SyntaxPredicateLeaf {
    public required IdentifierPath VariablePath { get; init; }
        
    public required HelixType UnionType { get; init; }
        
    public required UnionType UnionSignature { get; init; }

    public required ValueSet<string> MemberNames { get; init; }
        
    public override TypeFrame ApplyToTypes(TypeFrame types) {
        var mems = this.UnionSignature.Members
            .Select(x => x.Name)
            .ToValueSet();
            
        if (types.TryGetVariable(this.VariablePath, out var innerType)) {
            if (innerType is SingularUnionType otherUnion) {
                Debug.Assert(this.UnionType == otherUnion.UnionType);

                mems = otherUnion.MemberNames;
            }
        }

        // Narrow the possible members based on our predicate
        mems = mems.Intersect(this.MemberNames);
            
        var singType = new SingularUnionType {
            UnionType = this.UnionType,
            UnionSignature = this.UnionSignature,
            MemberNames = mems
        };

        return types.WithRefinement(this.VariablePath, new ReferenceType(singType));
    }

    public override bool TryOrWith(ISyntaxPredicate pred, out ISyntaxPredicate result) {
        if (pred is not IsUnionMemberPredicate other) {
            result = null;
            return false;
        }

        if (other.VariablePath != this.VariablePath || other.UnionSignature != this.UnionSignature) {
            result = null;
            return false;
        }

        result = new IsUnionMemberPredicate {
            VariablePath = this.VariablePath,
            UnionSignature = this.UnionSignature,
            UnionType = this.UnionType,
            MemberNames = this.MemberNames.Union(other.MemberNames)
        };

        return true;
    }

    public override bool TryAndWith(ISyntaxPredicate pred, out ISyntaxPredicate result) {
        if (pred is not IsUnionMemberPredicate other) {
            result = null;
            return false;
        }

        if (other.VariablePath != this.VariablePath || other.UnionSignature != this.UnionSignature) {
            result = null;
            return false;
        }

        var overlap = this.MemberNames.Intersect(other.MemberNames);

        if (overlap.Count == 0) {
            result = ISyntaxPredicate.Empty;
            return true;
        }
            
        result = new IsUnionMemberPredicate {
            VariablePath = this.VariablePath,
            UnionSignature = this.UnionSignature,
            UnionType = this.UnionType,
            MemberNames = overlap
        };

        return true;
    }

    public override ISyntaxPredicate Negate() {
        var result = new IsUnionMemberPredicate {
            VariablePath = this.VariablePath,
            UnionSignature = this.UnionSignature,
            UnionType = this.UnionType,
            MemberNames = this.UnionSignature.Members
                .Select(x => x.Name)
                .ToValueSet()
                .Except(this.MemberNames)
        };

        return result;
    }
        
    public override string ToString() {
        if (this.MemberNames.Count == 1) {
            return this.VariablePath.Segments.Last() + $" is {this.MemberNames.First()}";
        }
        else {
            return this.VariablePath.Segments.Last() + " is { " + string.Join("; ", this.MemberNames) + " }";
        }
    }
}