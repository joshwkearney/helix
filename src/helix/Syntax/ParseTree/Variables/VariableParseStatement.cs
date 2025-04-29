using Helix.Parsing;
using Helix.Syntax.ParseTree.FlowControl;
using Helix.Syntax.ParseTree.Structs;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record VariableParseStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IReadOnlyList<string> VariableNames { get; init; }
        
    public required IReadOnlyList<Option<IParseTree>> VariableTypes { get; init; }
        
    public required IParseTree Assignment { get; init; }
        
    public bool IsPure => false;
        
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        // Type check the assignment value
        (var assign, types) = this.Assignment.CheckTypes(types);
        
        // If this is a compound assignment, check if we have the right
        // number of names and then recurse
        if (this.VariableNames.Count > 1) {
            return this.Destructure(assign.ReturnType, types);
        }

        // Make sure assign can unify with our type expression
        if (this.VariableTypes[0].TryGetValue(out var typeSyntax)) {
            if (!typeSyntax.AsType(types).TryGetValue(out var type)) {
                throw TypeException.ExpectedTypeExpression(typeSyntax.Location);
            }

            assign = assign.UnifyTo(type, types);
        }

        // Make sure we're not shadowing anybody
        if (types.TryResolveName(types.Scope, this.VariableNames[0], out _)) {
            throw TypeException.IdentifierDefined(this.Location, this.VariableNames[0]);
        }

        var path = types.Scope.Append(this.VariableNames[0]);
        var sig = new PointerType(assign.ReturnType.GetSignature(types));

        types = types.WithDeclaration(path, new NominalType(path, NominalTypeKind.Variable));
        types = types.WithSignature(path, sig);
        types = types.WithValue(path, new PointerType(assign.ReturnType));

        var result = new VariableStatement {
            Location = this.Location,
            Path = path,
            Assignment = assign,
            VariableSignature = sig
        };

        return new(result, types);
    }
        
    private TypeCheckResult<ITypedStatement> Destructure(HelixType assignType, TypeFrame types) {
        if (!assignType.AsStruct(types).TryGetValue(out var sig)) {
            throw new TypeException(
                this.Location,
                "Invalid Desconstruction",
                $"Cannot deconstruct non-struct type '{assignType}'");
        }

        if (sig.Members.Count != this.VariableNames.Count) {
            throw new TypeException(
                this.Location,
                "Invalid Desconstruction",
                "The number of variables provided does not match "
              + $"the number of members on struct type '{assignType}'");
        }

        var tempName = types.GetVariableName();

        var tempStat = this with {
            VariableNames = [tempName],
            VariableTypes = [Option.None]
        };

        var stats = new List<IParseStatement> { tempStat };

        for (int i = 0; i < sig.Members.Count; i++) {
            var literal = new VariableAccessParseTree {
                Location = this.Location,
                VariableName = tempName
            };
            
            var access = new MemberAccessParseTree {
                Location = this.Location,
                MemberName = sig.Members[i].Name,
                Operand = literal
            };

            var assign = new VariableParseStatement {
                Location = this.Location,
                VariableNames = [this.VariableNames[i]],
                VariableTypes = [this.VariableTypes[i]],
                Assignment = access
            };

            stats.Add(assign);
        }

        var result = new BlockParseTree {
            Location = this.Location,
            Statements = stats
        };

        return result.CheckTypes(types);
    }
}