using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Structs {
    public record TypedStructDeclaration : IDeclaration {
        public required TokenLocation Location { get; init; }
        
        public required StructType Signature { get; init; }
        
        public required IdentifierPath Path { get; init; }

        public TypeFrame DeclareNames(TypeFrame types) => types;

        public TypeFrame DeclareTypes(TypeFrame types) => types;

        public TypeCheckResult<IDeclaration> CheckTypes(TypeFrame types) => new(this, types);

        public void GenerateIR(IRWriter writer, IRFrame context) {
        }
        
        public void GenerateCode(TypeFrame types, ICWriter writer) {
            var name = writer.GetVariableName(this.Path);

            var mems = this.Signature.Members
                .Select(x => new CParameter {
                    Type = writer.ConvertType(x.Type, types),
                    Name = x.Name
                })
                .ToArray();

            var prototype = new CAggregateDeclaration {
                Name = name
            };

            var fullDeclaration = new CAggregateDeclaration {
                Name = name,
                Members = mems
            };

            // Write forward declaration
            writer.WriteDeclaration1(prototype);

            // Write full struct
            writer.WriteDeclaration3(fullDeclaration);
            writer.WriteDeclaration3(new CEmptyLine());
        }
    }
}