using Helix.Parsing;
using Helix.Syntax.TypedTree.Primitives;
using Helix.Syntax.TypedTree.Unions;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Unions;

public class NewUnionParseTree : IParseTree {
    public required TokenLocation Location { get; init; }

    public required UnionType UnionSignature { get; init; }
    
    public required HelixType UnionType { get; init; }

    public IReadOnlyList<string> Names { get; init; } = [];

    public IReadOnlyList<IParseTree> Values { get; init; } = [];
        
    public bool IsPure => this.Values.All(x => x.IsPure);
        
    public TypeCheckResult CheckTypes(TypeFrame types) {
        if (this.Names.Count > 1 || this.Values.Count > 1) {
            throw new TypeException(
                this.Location,
                "Invalid Union Initialization",
                "Union initializers must have at most one argument.");
        }

        string name;
        if (this.Names.Count == 0 || this.Names[0] == null) {
            name = this.UnionSignature.Members[0].Name;
        }
        else {
            name = this.Names[0];
        }

        var mem = this.UnionSignature.Members.FirstOrDefault(x => x.Name == name);
        if (mem == null) {
            throw new TypeException(
                this.Location,
                "Invalid Union Initialization",
                $"The member '{name}' does not exist in the "
              + $"union type '{this.UnionSignature}'");
        }

        ITypedTree value;
        
        if (this.Values.Count == 0) {
            if (!mem.Type.HasDefaultValue(types)) {
                throw new TypeException(
                    this.Location,
                    "Invalid Union Initialization",
                    $"The union member '{name}' does not have a default value. " 
                  + "Please supply an explicit value or initialize the union " 
                  + "with a different member.");
            }

            (value, types) = new VoidLiteral { Location = this.Location }.CheckTypes(types);
        }
        else {
            (value, types) = this.Values[0].CheckTypes(types);
        }
        
        value = value.UnifyTo(mem.Type, types);

        var result = new NewUnionTypedTree {
            Location = this.Location,
            UnionSignature = this.UnionSignature,
            UnionType = this.UnionType,
            Name = name,
            Value = value,
            AlwaysJumps = value.AlwaysJumps
        };
                
        return new TypeCheckResult(result, types);
    }
}