using Helix.Collections;
using Helix.Parsing;
using Helix.Syntax.ParseTree.Variables;
using Helix.Syntax.TypedTree.Unions;
using Helix.TypeChecking;
using Helix.TypeChecking.Predicates;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Unions;

public record IsParseTree : IParseTree {
    public required TokenLocation Location { get; init; }

    public required IParseTree Operand { get; init; }

    public required string MemberName { get; init; }

    public bool IsPure => this.Operand.IsPure;
        
    public TypeCheckResult CheckTypes(TypeFrame types) {
        // TODO: Why can't we use this on arbitrary expressions???
        if (this.Operand is not VariableAccessParseTree access) {
            throw new TypeException(
                this.Operand.Location, 
                "Invalid 'is' syntax", 
                "Only variable names referring to unions can be used with the 'is' keyword.");
        }

        // Make sure this name exists
        if (!types.TryResolvePath(types.Scope, access.VariableName, out var path)) {
            throw TypeException.VariableUndefined(this.Location, access.VariableName);
        }

        // Make sure we have a variable
        if (!types.TryGetVariable(path, out var varSig)) {
            throw TypeException.VariableUndefined(this.Operand.Location, access.VariableName);
        }

        // Make sure we have a variable pointing to a union
        if (!varSig.InnerType.AsUnion(types).TryGetValue(out var unionSig)) {
            throw TypeException.ExpectedUnionType(this.Operand.Location);
        }

        // Make sure this union actually contains this member
        if (unionSig.Members.All(x => x.Name != this.MemberName)) {
            throw TypeException.MemberUndefined(
                this.Location,
                unionSig, 
                this.MemberName);
        }

        var predicate = new IsUnionMemberPredicate {
            VariablePath = path,
            MemberNames = new[] { this.MemberName }.ToValueSet(),
            UnionSignature = unionSig,
            UnionType = varSig.InnerType
        };

        var returnType = new PredicateBool(predicate);

        var result = new IsTypedTree {
            Location = this.Location,
            MemberName = this.MemberName,
            UnionSignature = unionSig,
            VariablePath = path,
            ReturnType = returnType
        };

        return new TypeCheckResult(result, types);
    }
}