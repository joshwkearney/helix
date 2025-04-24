using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Features.Functions {
    public record FunctionDeclaration : IDeclaration {
        public TokenLocation Location { get; init; }
        
        public required ISyntax Body { get; init; }

        public required FunctionType Signature { get; init; }

        public required IdentifierPath Path { get; init; }

        public void DeclareNames(TypeFrame names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeFrame paths) {
            throw new InvalidOperationException();
        }

        public IDeclaration CheckTypes(TypeFrame types) {
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
                .Prepend(new CParameter() {
                    Name = "_return_region",
                    Type = new CNamedType("_Region*")
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

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteEmptyLine();
                bodyWriter.WriteStatement(new CReturn() { 
                    Target = retExpr
                });
            }

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
