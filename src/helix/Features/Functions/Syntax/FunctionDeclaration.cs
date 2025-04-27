using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions.Syntax {
    public record FunctionDeclaration : IDeclaration {
        public TokenLocation Location { get; init; }
        
        public required ISyntax Body { get; init; }

        public required FunctionType Signature { get; init; }

        public required IdentifierPath Path { get; init; }

        public TypeFrame DeclareNames(TypeFrame names) {
            throw new InvalidOperationException();
        }

        public TypeFrame DeclareTypes(TypeFrame paths) {
            throw new InvalidOperationException();
        }

        public DeclarationTypeCheckResult CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public void GenerateCode(TypeFrame types, ICWriter writer) {
            writer.ResetTempNames();

            var returnType = this.Signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.Signature.ReturnType, types);

            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter() { 
                    Type = writer.ConvertType(x.Type, types),
                    Name = writer.GetVariableName(this.Path.Append(x.Name))
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.Path);
            var body = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, body);

            // Register the parameters as local variables
            foreach (var par in this.Signature.Parameters) {
                foreach (var (relPath, _) in par.Type.GetMembers(types)) {
                    var path = this.Path.Append(par.Name).Append(relPath);

                    bodyWriter.VariableKinds[path] = CVariableKind.Local;
                }
            }

            // Generate the body
            var retExpr = this.Body.GenerateCode(types, bodyWriter);

            // If the body ends with an empty line, trim it
            if (body.Any() && body.Last().IsEmpty) {
                body = body.SkipLast(1).ToList();
            }

            var decl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars,
                Body = body
            };

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration4(decl);
            writer.WriteDeclaration4(new CEmptyLine());
        }
    }
}
