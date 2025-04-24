using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Types;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Features.Structs {
    public record StructDeclaration : IDeclaration {
        public required TokenLocation Location { get; init; }
        
        public required StructType Signature { get; init; }
        
        public required IdentifierPath Path { get; init; }

        public void DeclareNames(TypeFrame types) { }

        public void DeclareTypes(TypeFrame types) { }

        public IDeclaration CheckTypes(TypeFrame types) => this;

        public void GenerateCode(TypeFrame types, ICWriter writer) {
            var name = writer.GetVariableName(this.Path);

            var mems = this.Signature.Members
                .Select(x => new CParameter() {
                    Type = writer.ConvertType(x.Type, types),
                    Name = x.Name
                })
                .ToArray();

            var prototype = new CAggregateDeclaration() {
                Name = name
            };

            var fullDeclaration = new CAggregateDeclaration() {
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