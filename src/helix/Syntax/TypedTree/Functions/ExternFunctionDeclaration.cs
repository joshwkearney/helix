using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Functions {
    public record ExternFunctionDeclaration : IDeclaration {
        public required FunctionType Signature { get; init; }

        public required TokenLocation Location { get; init; }

        public required IdentifierPath Path { get; init; }
        
        public TypeFrame DeclareNames(TypeFrame names) {
            throw new InvalidOperationException();
        }

        public TypeFrame DeclareTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public DeclarationTypeCheckResult CheckTypes(TypeFrame types) => new(this, types);

        public void GenerateCode(TypeFrame types, ICWriter writer) {
            var returnType = this.Signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.Signature.ReturnType, types);

            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter() {
                    Type = writer.ConvertType(x.Type, types),
                    Name = writer.GetVariableName(this.Path.Append(x.Name))
                })
                .Prepend(new CParameter() {
                    Name = "_region",
                    Type = new CNamedType("int")
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.Path);

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);
        }
    }
}
