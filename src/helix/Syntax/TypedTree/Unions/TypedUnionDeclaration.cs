using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Unions;

public record TypedUnionDeclaration : IDeclaration {
    private readonly UnionSignature signature;
    private readonly IdentifierPath path;

    public TokenLocation Location { get; }

    public TypedUnionDeclaration(TokenLocation loc, UnionSignature sig, IdentifierPath path) {
        this.Location = loc;
        this.signature = sig;
        this.path = path;
    }

    public TypeFrame DeclareNames(TypeFrame types) => types;

    public TypeFrame DeclareTypes(TypeFrame types) => types;

    public TypeCheckResult<IDeclaration> CheckTypes(TypeFrame types) => new(this, types);

    public void GenerateCode(TypeFrame types, ICWriter writer) { 
        var structName = writer.GetVariableName(this.path);
        var unionName = writer.GetVariableName(this.path) + "_$Union";

        var unionPrototype = new CAggregateDeclaration {
            Name = unionName,
            IsUnion = true
        };

        var unionDeclaration = new CAggregateDeclaration {
            Name = unionName,
            IsUnion = true,
            Members = this.signature.Members
                .Select(x => new CParameter {
                    Type = writer.ConvertType(x.Type, types),
                    Name = x.Name
                })
                .ToArray(),
        };

        var structPrototype = new CAggregateDeclaration {
            Name = structName
        };

        var structDeclaration = new CAggregateDeclaration {
            Name = structName,
            Members = new[] {
                new CParameter {
                    Name = "tag",
                    Type = new CNamedType("int")
                },
                new CParameter {
                    Name = "data",
                    Type = new CNamedType(unionName)
                }
            }
        };

        // Write forward declaration
        writer.WriteDeclaration1(unionPrototype);
        writer.WriteDeclaration1(structPrototype);

        // Write full struct
        writer.WriteDeclaration3(unionDeclaration);
        writer.WriteDeclaration3(new CEmptyLine());

        writer.WriteDeclaration3(structDeclaration);
        writer.WriteDeclaration3(new CEmptyLine());
    }
}